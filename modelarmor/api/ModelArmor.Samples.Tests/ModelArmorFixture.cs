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
using Google.Api.Gax.ResourceNames;
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
    public string ProjectId { get; }
    public string LocationId { get; }
    public string InspectTemplateId { get; }
    public string DeidentifyTemplateId { get; }

    // // Template IDs for SDP testing

    // Track resources to clean up
    private readonly List<TemplateName> _resourcesToCleanup = new List<TemplateName>();

    public string InspectTemplateId => "dlp-inspect-template-1";
    public string DeidentifyTemplateId => "dlp-deidentify-template-1";

    public ModelArmorFixture()
    {
        // Get the Google Cloud ProjectId
        ProjectId = GetRequiredEnvVar(EnvProjectId);

        // Get location ID from environment variable or use default
        LocationId = Environment.GetEnvironmentVariable(EnvLocation) ?? "us-central1";

        InspectTemplateId =
            Environment.GetEnvironmentVariable(EnvInspectTemplateId) ?? "dlp-inspect-template-1";
        DeidentifyTemplateId =
            Environment.GetEnvironmentVariable(EnvDeidentifyTemplateId)
            ?? "dlp-deidentify-template-1";

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
