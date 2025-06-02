/*
 * Copyright 2025 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     https://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.Dlp.V2;
using Google.Cloud.ModelArmor.V1;
using Xunit;

[CollectionDefinition(nameof(ModelArmorFixture))]
public class ModelArmorFixture : IDisposable, ICollectionFixture<ModelArmorFixture>
{
    // Environment variable names
    private const string EnvProjectId = "GOOGLE_PROJECT_ID";
    private const string EnvLocation = "GOOGLE_CLOUD_LOCATION";
    private const string EnvInspectTemplateId = "GOOGLE_CLOUD_INSPECT_TEMPLATE_ID";
    private const string EnvDeidentifyTemplateId = "GOOGLE_CLOUD_DEIDENTIFY_TEMPLATE_ID";

    // Public properties
    public Google.Cloud.ModelArmor.V1.ModelArmorClient Client { get; }
    public DlpServiceClient DlpClient { get; }
    public string ProjectId { get; }
    public string LocationId { get; }
    public string InspectTemplateId { get; }
    public string DeidentifyTemplateId { get; }

    // // Template IDs for SDP testing

    // Track resources to clean up
    private readonly List<TemplateName> _resourcesToCleanup = new List<TemplateName>();

    public ModelArmorFixture()
    {
        // Get the Google Cloud ProjectId
        ProjectId = GetRequiredEnvVar(EnvProjectId);

        // Get location ID from environment variable or use default
        LocationId = Environment.GetEnvironmentVariable(EnvLocation) ?? "us-central1";

        // Get template ID from environment variable or generate a unique one
        DlpClient = DlpServiceClient.Create();

        // Info Types:
        // https://cloud.google.com/sensitive-data-protection/docs/infotypes-reference
        List<InfoType> infoTypes = new List<string>
        {
            "PHONE_NUMBER",
            "EMAIL_ADDRESS",
            "US_INDIVIDUAL_TAXPAYER_IDENTIFICATION_NUMBER",
        }
            .Select(it => new InfoType { Name = it })
            .ToList();

        InspectConfig inspectConfig = new InspectConfig { InfoTypes = { infoTypes } };

        InspectTemplate inspectTemplate = new InspectTemplate { InspectConfig = inspectConfig };

        CreateInspectTemplateRequest createInspectTemplateRequest = new CreateInspectTemplateRequest
        {
            ParentAsLocationName = new LocationName(ProjectId, LocationId),
            InspectTemplate = inspectTemplate,
            TemplateId = GenerateUniqueId(),
        };

        InspectTemplateId = DlpClient
            .CreateInspectTemplate(createInspectTemplateRequest)
            .InspectTemplateName.InspectTemplateId;

        var replaceValueConfig = new ReplaceValueConfig
        {
            NewValue = new Value { StringValue = "[REDACTED]" },
        };

        // Define type of deidentification.
        var primitiveTransformation = new PrimitiveTransformation
        {
            ReplaceConfig = replaceValueConfig,
        };

        // Associate deidentification type with info type.
        var transformation = new InfoTypeTransformations.Types.InfoTypeTransformation
        {
            PrimitiveTransformation = primitiveTransformation,
        };

        // Construct the configuration for the Redact request and list all desired transformations.
        var redactConfig = new DeidentifyConfig
        {
            InfoTypeTransformations = new InfoTypeTransformations
            {
                Transformations = { transformation },
            },
        };

        var deidentifyTemplate = new DeidentifyTemplate { DeidentifyConfig = redactConfig };

        var createDeidentifyTemplateRequest = new CreateDeidentifyTemplateRequest
        {
            ParentAsLocationName = LocationName.FromProjectLocation(ProjectId, LocationId),
            TemplateId = GenerateUniqueId(),
            DeidentifyTemplate = deidentifyTemplate,
        };

        DeidentifyTemplateId = DlpClient
            .CreateDeidentifyTemplate(createDeidentifyTemplateRequest)
            .DeidentifyTemplateName.DeidentifyTemplateId;

        // Create client builder
        ModelArmorClientBuilder clientBuilder = new ModelArmorClientBuilder
        {
            Endpoint = $"modelarmor.{LocationId}.rep.googleapis.com",
        };

        // Create the client.
        Client = clientBuilder.Build();
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

        // Reset floor settings to default
        ResetFloorSettings();

        // Delete created DLP templates
        DlpClient.DeleteInspectTemplate(
            $"projects/{ProjectId}/locations/{LocationId}/inspectTemplates/{InspectTemplateId}"
        );
        DlpClient.DeleteDeidentifyTemplate(
            $"projects/{ProjectId}/locations/{LocationId}/deidentifyTemplates/{DeidentifyTemplateId}"
        );
    }

    // Reset floor settings to default values for project, folder, and organization
    private void ResetFloorSettings()
    {
        // Reset project floor settings if project ID is available
        if (!string.IsNullOrEmpty(ProjectId))
        {
            ResetProjectFloorSettings(ProjectId);
        }

        // Reset folder floor settings if folder ID is available
        string folderId = Environment.GetEnvironmentVariable("MA_FOLDER_ID");
        if (!string.IsNullOrEmpty(folderId))
        {
            ResetFolderFloorSettings(folderId);
        }

        // Reset organization floor settings if organization ID is available
        string organizationId = Environment.GetEnvironmentVariable("MA_ORG_ID");
        if (!string.IsNullOrEmpty(organizationId))
        {
            ResetOrganizationFloorSettings(organizationId);
        }
    }

    // Reset project floor settings to default
    public void ResetProjectFloorSettings(string projectId)
    {
        try
        {
            // Add a small delay to avoid rate limiting
            System.Threading.Thread.Sleep(2000);

            // Create default floor setting with empty RAI filters and enforcement disabled
            FloorSetting defaultFloorSetting = new FloorSetting
            {
                Name = $"projects/{projectId}/locations/global/floorSetting",
                FilterConfig = new FilterConfig
                {
                    RaiSettings = new RaiFilterSettings { RaiFilters = { } },
                },
                EnableFloorSettingEnforcement = false,
            };

            // Update the floor setting to reset it
            Client.UpdateFloorSetting(
                new UpdateFloorSettingRequest { FloorSetting = defaultFloorSetting }
            );
        }
        catch (Exception ex)
        {
            // Log but don't throw to avoid breaking test cleanup
            Console.WriteLine($"Error resetting project floor settings: {ex.Message}");
        }
    }

    // Reset folder floor settings to default
    public void ResetFolderFloorSettings(string folderId)
    {
        try
        {
            // Add a small delay to avoid rate limiting
            System.Threading.Thread.Sleep(2000);

            // Create default floor setting with empty RAI filters and enforcement disabled
            FloorSetting defaultFloorSetting = new FloorSetting
            {
                Name = $"folders/{folderId}/locations/global/floorSetting",
                FilterConfig = new FilterConfig
                {
                    RaiSettings = new RaiFilterSettings { RaiFilters = { } },
                },
                EnableFloorSettingEnforcement = false,
            };

            // Update the floor setting to reset it
            Client.UpdateFloorSetting(
                new UpdateFloorSettingRequest { FloorSetting = defaultFloorSetting }
            );
        }
        catch (Exception ex)
        {
            // Log but don't throw to avoid breaking test cleanup
            Console.WriteLine($"Error resetting folder floor settings: {ex.Message}");
        }
    }

    // Reset organization floor settings to default
    public void ResetOrganizationFloorSettings(string organizationId)
    {
        try
        {
            // Add a small delay to avoid rate limiting
            System.Threading.Thread.Sleep(2000);

            // Create default floor setting with empty RAI filters and enforcement disabled
            FloorSetting defaultFloorSetting = new FloorSetting
            {
                Name = $"organizations/{organizationId}/locations/global/floorSetting",
                FilterConfig = new FilterConfig
                {
                    RaiSettings = new RaiFilterSettings { RaiFilters = { } },
                },
                EnableFloorSettingEnforcement = false,
            };

            // Update the floor setting to reset it
            Client.UpdateFloorSetting(
                new UpdateFloorSettingRequest { FloorSetting = defaultFloorSetting }
            );
        }
        catch (Exception ex)
        {
            // Log but don't throw to avoid breaking test cleanup
            Console.WriteLine($"Error resetting organization floor settings: {ex.Message}");
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

    public Template ConfigureBaseTemplateWithMetadata()
    {
        // First create a base template
        Template template = ConfigureBaseTemplate();

        // Configure Metadata settings
        Template.Types.TemplateMetadata templateMetadata = new Template.Types.TemplateMetadata
        {
            LogTemplateOperations = true,
            LogSanitizeOperations = true,
        };
        template.TemplateMetadata = templateMetadata;

        return template;
    }

    // Create a template with labels
    public Template ConfigureBaseTemplateWithLabels()
    {
        // First create a base template
        Template template = ConfigureBaseTemplate();

        // Add labels
        template.Labels.Add("key1", "value1");
        template.Labels.Add("key2", "value2");

        return template;
    }

    public Template ConfigureTemplateWithMaliciousUri()
    {
        Template template = ConfigureBaseTemplate();

        template.FilterConfig.MaliciousUriFilterSettings = new MaliciousUriFilterSettings
        {
            FilterEnforcement = MaliciousUriFilterSettings
                .Types
                .MaliciousUriFilterEnforcement
                .Enabled,
        };

        return template;
    }

    public Template ConfigureTemplateWithPiAndJailbreak()
    {
        Template template = ConfigureBaseTemplate();

        template.FilterConfig.PiAndJailbreakFilterSettings = new PiAndJailbreakFilterSettings
        {
            ConfidenceLevel = DetectionConfidenceLevel.MediumAndAbove,
            FilterEnforcement = PiAndJailbreakFilterSettings
                .Types
                .PiAndJailbreakFilterEnforcement
                .Enabled,
        };
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

    // Create a template with metadata on GCP
    public Template CreateTemplateWithMetadata(string templateId = null)
    {
        Template templateConfig = ConfigureBaseTemplateWithMetadata();
        return CreateTemplate(templateConfig, templateId);
    }

    // Create a template with labels on GCP
    public Template CreateTemplateWithLabels(string templateId = null)
    {
        Template templateConfig = ConfigureBaseTemplateWithLabels();
        return CreateTemplate(templateConfig, templateId);
    }

    public Template CreateTemplateWithMaliciousUri(string templateId = null)
    {
        Template templateConfig = ConfigureTemplateWithMaliciousUri();
        return CreateTemplate(templateConfig, templateId);
    }

    public Template CreateTemplateWithPiAndJailbreak(string templateId = null)
    {
        Template templateConfig = ConfigureTemplateWithPiAndJailbreak();
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
