using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Reflection;
using Microsoft.AspNetCore.Razor.Language;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using JabbR.Infrastructure;
using Microsoft.AspNetCore.Razor.Language;

namespace JabbR.Services
{
    public class RazorEmailTemplateEngine : IEmailTemplateEngine
    {
        public const string DefaultSharedTemplateSuffix = "";
        public const string DefaultHtmlTemplateSuffix = "html";
        public const string DefaultTextTemplateSuffix = "text";

        private const string NamespaceName = "JabbR.Views.EmailTemplates";

        private static readonly string[] _referencedAssemblies = BuildReferenceList().ToArray();
        private static readonly RazorProjectEngine _razorEngine = CreateRazorEngine();
        private static readonly Dictionary<string, IDictionary<string, Type>> _typeMapping = new Dictionary<string, IDictionary<string, Type>>(StringComparer.OrdinalIgnoreCase);
        private static readonly ReaderWriterLockSlim _syncLock = new ReaderWriterLockSlim();

        private readonly IEmailTemplateContentReader _contentReader;
        private readonly string _sharedTemplateSuffix;
        private readonly string _htmlTemplateSuffix;
        private readonly string _textTemplateSuffix;
        private readonly IDictionary<string, string> _templateSuffixes;

        public RazorEmailTemplateEngine(IEmailTemplateContentReader contentReader)
            : this(contentReader, DefaultSharedTemplateSuffix, DefaultHtmlTemplateSuffix, DefaultTextTemplateSuffix)
        {
            _contentReader = contentReader;
        }

        public RazorEmailTemplateEngine(IEmailTemplateContentReader contentReader, string sharedTemplateSuffix, string htmlTemplateSuffix, string textTemplateSuffix)
        {
            if (contentReader == null)
            {
                throw new ArgumentNullException("contentReader");
            }

            _contentReader = contentReader;
            _sharedTemplateSuffix = sharedTemplateSuffix;
            _htmlTemplateSuffix = htmlTemplateSuffix;
            _textTemplateSuffix = textTemplateSuffix;
            _templateSuffixes = new Dictionary<string, string>
                                {
                                    { _sharedTemplateSuffix, String.Empty },
                                    { _htmlTemplateSuffix, ContentTypes.Html },
                                    { _textTemplateSuffix, ContentTypes.Text }
                                };
        }

        public Email RenderTemplate(string templateName, object model = null)
        {
            if (String.IsNullOrWhiteSpace(templateName))
            {
                throw new System.ArgumentException(String.Format(System.Globalization.CultureInfo.CurrentUICulture, "\"{0}\" cannot be blank.", "templateName"));
            }

            var templates = CreateTemplateInstances(templateName);

            foreach (var pair in templates)
            {
                pair.Value.SetModel(CreateModel(model));
                pair.Value.Execute();
            }

            var mail = new Email();

            templates.SelectMany(x => x.Value.To)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Each(email => mail.To.Add(email));

            templates.SelectMany(x => x.Value.ReplyTo)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Each(email => mail.ReplyTo.Add(email));

            templates.SelectMany(x => x.Value.Bcc)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Each(email => mail.Bcc.Add(email));

            templates.SelectMany(x => x.Value.CC)
                     .Distinct(StringComparer.OrdinalIgnoreCase)
                     .Each(email => mail.CC.Add(email));

            IEmailTemplate template = null;

            // text template (.text.cshtml file)
            if (templates.TryGetValue(ContentTypes.Text, out template))
            {
                SetProperties(template, mail, body => { mail.TextBody = body; });
            }
            // html template (.html.cshtml file)
            if (templates.TryGetValue(ContentTypes.Html, out template))
            {
                SetProperties(template, mail, body => { mail.HtmlBody = body; });
            }
            // shared template (.cshtml file)
            if (templates.TryGetValue(String.Empty, out template))
            {
                SetProperties(template, mail, null);
            }

            return mail;
        }

        private IDictionary<string, IEmailTemplate> CreateTemplateInstances(string templateName)
        {
            return GetTemplateTypes(templateName).Select(pair => new { ContentType = pair.Key, Template = (IEmailTemplate)Activator.CreateInstance(pair.Value) })
                                                 .ToDictionary(k => k.ContentType, e => e.Template);
        }

        private IDictionary<string, Type> GetTemplateTypes(string templateName)
        {
            IDictionary<string, Type> templateTypes;

            _syncLock.EnterUpgradeableReadLock();

            try
            {
                if (!_typeMapping.TryGetValue(templateName, out templateTypes))
                {
                    _syncLock.EnterWriteLock();

                    try
                    {
                        templateTypes = GenerateTemplateTypes(templateName);
                        _typeMapping.Add(templateName, templateTypes);
                    }
                    finally
                    {
                        _syncLock.ExitWriteLock();
                    }
                }
            }
            finally
            {
                _syncLock.ExitUpgradeableReadLock();
            }

            return templateTypes;
        }

        private IDictionary<string, Type> GenerateTemplateTypes(string templateName)
        {
            var templates = _templateSuffixes.Select(pair => new
                                                    {
                                                        Suffix = pair.Key,
                                                        TemplateName = templateName + pair.Key,
                                                        Content = _contentReader.Read(templateName, pair.Key),
                                                        ContentType = pair.Value
                                                    })
                                             .Where(x => !String.IsNullOrWhiteSpace(x.Content))
                                             .ToList();

            var compilableTemplates = templates.Select(x => new KeyValuePair<string, string>(x.TemplateName, x.Content)).ToArray();
            var assembly = GenerateAssembly(compilableTemplates);

            return templates.Select(x => new { ContentType = x.ContentType, Type = assembly.GetType(NamespaceName + "." + x.TemplateName, true, false) })
                            .ToDictionary(k => k.ContentType, e => e.Type);
        }

        private static void SetProperties(IEmailTemplate template, Email mail, Action<string> updateBody)
        {
            if (template != null)
            {
                if (!String.IsNullOrWhiteSpace(template.From))
                {
                    mail.From = template.From;
                }

                if (!String.IsNullOrWhiteSpace(template.Sender))
                {
                    mail.Sender = template.Sender;
                }

                if (!String.IsNullOrWhiteSpace(template.Subject))
                {
                    mail.Subject = template.Subject;
                }

                template.Headers.Each(pair => mail.Headers[pair.Key] = pair.Value);

                if (updateBody != null)
                {
                    updateBody(template.Body);
                }
            }
        }

        private static Assembly GenerateAssembly(params KeyValuePair<string, string>[] templates)
        {
            var templateResults = templates.Select(pair => {
                var codeDocument = _razorEngine.Process(RazorSourceDocument.Create(pair.Value, pair.Key));
                return codeDocument.GetCSharpDocument();
            }).ToList();

            if (templateResults.Any(result => result.Diagnostics.Any(d => d.Severity == RazorDiagnosticSeverity.Error)))
            {
                var parseExceptionMessage = String.Join(Environment.NewLine + Environment.NewLine,
                    templateResults.SelectMany(r => r.Diagnostics)
                                   .Where(d => d.Severity == RazorDiagnosticSeverity.Error)
                                   .Select(e => e.GetMessage()));

                throw new InvalidOperationException(parseExceptionMessage);
            }

            using (var codeProvider = new CSharpCodeProvider())
            {
                var compilerParameter = new CompilerParameters(_referencedAssemblies)
                                            {
                                                IncludeDebugInformation = false,
                                                GenerateInMemory = true,
                                                CompilerOptions = "/optimize"
                                            };

                var compilerResults = codeProvider.CompileAssemblyFromDom(compilerParameter, templateResults.Select(r => r.GeneratedCode).ToArray());

                if (compilerResults.Errors.HasErrors)
                {
                    var compileExceptionMessage = String.Join(Environment.NewLine + Environment.NewLine, compilerResults.Errors.OfType<CompilerError>().Where(ce => !ce.IsWarning).Select(e => e.FileName + ":" + Environment.NewLine + e.ErrorText).ToArray());

                    throw new InvalidOperationException(compileExceptionMessage);
                }

                return compilerResults.CompiledAssembly;
            }
        }

        private static dynamic CreateModel(object model)
        {
            if (model == null)
            {
                return null;
            }

            if (model is IDynamicMetaObjectProvider)
            {
                return model;
            }

            var propertyMap = model.GetType()
                                   .GetProperties()
                                   .Where(property => property.CanRead && property.GetIndexParameters().Length == 0)
                                   .ToDictionary(property => property.Name, property => property.GetValue(model, null));

            return new DynamicModel(propertyMap);
        }

        private static RazorProjectEngine CreateRazorEngine()
        {
            var builder = RazorProjectEngine.Create(RazorConfiguration.Default, RazorProjectFileSystem.Create("."), b =>
            {
                b.SetNamespace(NamespaceName);
                b.SetBaseType(typeof(EmailTemplate).FullName);
                b.ConfigureClass((document, @class) =>
                {
                    @class.ClassName = document.Source.FilePath.Replace(".cshtml", string.Empty);
                });
            });

            return builder;
        }

        private static IEnumerable<string> BuildReferenceList()
        {
            return new List<string>
            {
                typeof(object).Assembly.Location,
                typeof(Enumerable).Assembly.Location,
                typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location,
                typeof(Microsoft.CSharp.RuntimeBinder.CSharpArgumentInfo).Assembly.Location,
                typeof(System.Dynamic.DynamicObject).Assembly.Location,
                typeof(RazorEmailTemplateEngine).Assembly.Location
            };
        }
    }
}