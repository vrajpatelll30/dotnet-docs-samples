using System;
using System.Collections.Generic;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.ModelArmor.V1;
using Xunit;

[CollectionDefinition(nameof(ModelArmorFixture))]
public class ModelArmorFixture : IDisposable, ICollectionFixture<ModelArmorFixture>
{
    // Environment variable names
    private const string EnvProjectId = "GOOGLE_PROJECT_ID";
    private const string EnvLocation = "GOOGLE_CLOUD_LOCATION";

    // Public properties
    public ModelArmorClient Client { get; }
    public string ProjectId { get; }
    public string LocationId { get; }

    // Template IDs for SDP testing
    public string InspectTemplateId => "dlp-inspect-template-1";
    public string DeidentifyTemplateId => "dlp-deidentify-template-1";

    // Track resources to clean up
    private readonly List<TemplateName> _resourcesToCleanup = new List<TemplateName>();

    public string InspectTemplateId => "dlp-inspect-template-1";
    public string DeidentifyTemplateId => "dlp-deidentify-template-1";

    public ModelArmorFixture()
    {
        // Get the Google Cloud ProjectId
        ProjectId = GetRequiredEnvVar(EnvProjectId);

        // Get location ID from environment variable or use default
        LocationId = GetRequiredEnvVar(EnvLocation) ?? "us-central1";

        // Create client
        Client = ModelArmorClient.Create();
    }

    private string GetRequiredEnvVar(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);
        if (string.IsNullOrEmpty(value))
        {
            throw new Exception($"Missing {name} environment variable");
        }
        return value;
    }

    public string GenerateUniqueId()
    {
        return Guid.NewGuid().ToString("N").Substring(0, 8);
    }

    public void Dispose()
    {
        // Clean up resources after tests
        foreach (var resourceName in _resourcesToCleanup)
        {
            try
            {
                Client.DeleteTemplate(new DeleteTemplateRequest { TemplateName = resourceName });
            }
            catch (Exception)
            {
                // Ignore errors during cleanup
            }
        }
    }

    public void RegisterTemplateForCleanup(TemplateName templateName)
    {
        if (templateName != null && !string.IsNullOrEmpty(templateName.ToString()))
        {
            _resourcesToCleanup.Add(templateName);
        }
    }

    public TemplateName CreateTemplateName(string prefix = "test")
    {
        string templateId = $"{prefix}-{GenerateUniqueId()}";
        return new TemplateName(ProjectId, LocationId, templateId);
    }

    // Base method to create a template with basic RAI settings
    public Template ConfigureBaseTemplate()
    {
        RaiFilterSettings raiFilterSettings = new RaiFilterSettings();
        raiFilterSettings.RaiFilters.Add(
            new RaiFilterSettings.Types.RaiFilter
            {
                FilterType = RaiFilterType.Dangerous,
                ConfidenceLevel = DetectionConfidenceLevel.High,
            }
        );
        raiFilterSettings.RaiFilters.Add(
            new RaiFilterSettings.Types.RaiFilter
            {
                FilterType = RaiFilterType.HateSpeech,
                ConfidenceLevel = DetectionConfidenceLevel.MediumAndAbove,
            }
        );
        raiFilterSettings.RaiFilters.Add(
            new RaiFilterSettings.Types.RaiFilter
            {
                FilterType = RaiFilterType.SexuallyExplicit,
                ConfidenceLevel = DetectionConfidenceLevel.MediumAndAbove,
            }
        );
        raiFilterSettings.RaiFilters.Add(
            new RaiFilterSettings.Types.RaiFilter
            {
                FilterType = RaiFilterType.Harassment,
                ConfidenceLevel = DetectionConfidenceLevel.MediumAndAbove,
            }
        );

        // Create the filter config with RAI settings
        FilterConfig modelArmorFilter = new FilterConfig { RaiSettings = raiFilterSettings };

        // Create the template
        Template template = new Template { FilterConfig = modelArmorFilter };
        return template;
    }

    // Create a template with Basic SDP configuration
    public Template ConfigureBasicSdpTemplate()
    {
        // First create a base template
        Template template = ConfigureBaseTemplate();

        // Add Basic SDP configuration
        SdpBasicConfig basicSdpConfig = new SdpBasicConfig
        {
            FilterEnforcement = SdpBasicConfig.Types.SdpBasicConfigEnforcement.Enabled,
        };

        SdpFilterSettings sdpSettings = new SdpFilterSettings { BasicConfig = basicSdpConfig };
        template.FilterConfig.SdpSettings = sdpSettings;

        return template;
    }

    // Create a template with Advanced SDP configuration
    public Template ConfigureAdvancedSdpTemplate()
    {
        // First create a base template
        Template template = ConfigureBaseTemplate();

        // Add Advanced SDP configuration
        string inspectTemplateName =
            $"projects/{ProjectId}/locations/{LocationId}/inspectTemplates/{InspectTemplateId}";

        string deidentifyTemplateName =
            $"projects/{ProjectId}/locations/{LocationId}/deidentifyTemplates/{DeidentifyTemplateId}";

        SdpAdvancedConfig advancedSdpConfig = new SdpAdvancedConfig
        {
            InspectTemplate = inspectTemplateName,
            DeidentifyTemplate = deidentifyTemplateName,
        };

        SdpFilterSettings sdpSettings = new SdpFilterSettings
        {
            AdvancedConfig = advancedSdpConfig,
        };
        template.FilterConfig.SdpSettings = sdpSettings;

        return template;
    }

    // Create a template on GCP and register it for cleanup
    public Template CreateTemplate(Template templateConfig, string templateId = null)
    {
        // Generate a unique template ID if none provided
        templateId ??= $"test-{GenerateUniqueId()}";

        // Create the parent resource name
        LocationName parent = LocationName.FromProjectLocation(ProjectId, LocationId);

        // Create the template
        Template createdTemplate = Client.CreateTemplate(
            new CreateTemplateRequest
            {
                ParentAsLocationName = parent,
                Template = templateConfig,
                TemplateId = templateId,
            }
        );

        // Register the template for cleanup
        RegisterTemplateForCleanup(TemplateName.Parse(createdTemplate.Name));

        return createdTemplate;
    }

    // Create a base template on GCP
    public Template CreateBaseTemplate(string templateId = null)
    {
        Template templateConfig = ConfigureBaseTemplate();
        return CreateTemplate(templateConfig, templateId);
    }

    // Create a template with Basic SDP on GCP
    public Template CreateBasicSdpTemplate(string templateId = null)
    {
        Template templateConfig = ConfigureBasicSdpTemplate();
        return CreateTemplate(templateConfig, templateId);
    }

    // Create a template with Advanced SDP on GCP
    public Template CreateAdvancedSdpTemplate(string templateId = null)
    {
        Template templateConfig = ConfigureAdvancedSdpTemplate();
        return CreateTemplate(templateConfig, templateId);
    }

    // Get a template by name
    public Template GetTemplate(TemplateName templateName)
    {
        GetTemplateRequest request = new GetTemplateRequest { TemplateName = templateName };

        return Client.GetTemplate(request);
    }

    // Delete a template by name
    public void DeleteTemplate(TemplateName templateName)
    {
        DeleteTemplateRequest request = new DeleteTemplateRequest { TemplateName = templateName };

        Client.DeleteTemplate(request);

        // Remove from cleanup list since we've already deleted it
        _resourcesToCleanup.Remove(templateName);
    }

    // Update an existing template
    public Template UpdateTemplate(Template template)
    {
        UpdateTemplateRequest request = new UpdateTemplateRequest { Template = template };

        return Client.UpdateTemplate(request);
    }

    // Create the full resource name for a template
    public string GetTemplateResourceName(string templateId)
    {
        return TemplateName
            .FromProjectLocationTemplate(ProjectId, LocationId, templateId)
            .ToString();
    }
}
