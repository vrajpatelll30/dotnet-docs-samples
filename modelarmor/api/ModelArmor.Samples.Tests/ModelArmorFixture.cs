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
    public ModelArmorClient Client { get; }
    public string ProjectId { get; }
    public string LocationId { get; }
    public string TemplateId { get; }
    public TemplateName TemplateForQuickstartName { get; }

    public string InspectTemplateId => "dlp-inspect-template-1";
    public string DeidentifyTemplateId => "dlp-deidentify-template-1";

    public ModelArmorFixture()
    {
        // Get the Google Cloud ProjectId
        ProjectId = Environment.GetEnvironmentVariable("GOOGLE_PROJECT_ID");
        if (String.IsNullOrEmpty(ProjectId))
        {
            throw new Exception("missing GOOGLE_PROJECT_ID");
        }

        // Get LocationId (e.g., "us-west1")
        LocationId = Environment.GetEnvironmentVariable("GOOGLE_CLOUD_LOCATION") ?? "us-west1";

        // Create the Model Armor Client
        Client = ModelArmorClient.Create();

        TemplateId = $"test-template-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
        TemplateForQuickstartName = TemplateName.FromProjectLocationTemplate(
            ProjectId,
            LocationId,
            TemplateId
        );
    }

    public void Dispose()
    {
        // Clean up resources after tests
        try
        {
            Client.DeleteTemplate(
                new DeleteTemplateRequest { Name = TemplateForQuickstartName.ToString() }
            );
        }
        catch (Exception)
        {
            // Ignore errors during cleanup
        }
    }

    public Template CreateTemplateConfiguration()
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

        // Create base template configuration
        Template template = new Template { FilterConfig = modelArmorFilter };

        return template;
    }

    public string CreateTemplate()
    {
        Template template = CreateTemplateConfiguration();
        CreateTemplateRequest request = new CreateTemplateRequest
        {
            ParentAsLocationName = LocationName.FromProjectLocation(ProjectId, LocationId),
            TemplateId = TemplateId,
            Template = template,
        };
        Template createdTemplate = Client.CreateTemplate(request);

        return createdTemplate.Name;
    }
}
