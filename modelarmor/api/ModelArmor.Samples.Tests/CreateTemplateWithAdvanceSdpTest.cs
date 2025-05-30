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
using Google.Cloud.Dlp.V2;
using Google.Cloud.ModelArmor.V1;
using Xunit;
using Xunit.Abstractions;

namespace ModelArmor.Samples.Tests
{
    public class CreateTemplateWithAdvancedSdpTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly CreateTemplateWithAdvancedSdpSample _sample;
        private readonly ITestOutputHelper _output;

        public CreateTemplateWithAdvancedSdpTests(
            ModelArmorFixture fixture,
            ITestOutputHelper output
        )
        {
            _fixture = fixture;
            _sample = new CreateTemplateWithAdvancedSdpSample();
            _output = output;
        }

        [Fact]
        public void CreateTemplateWithAdvancedSdpTest()
        {
            string inspectTemplateName = _fixture.InspectTemplateName;
            string deidentifyTemplateName = _fixture.DeidentifyTemplateName;

            string projectId = _fixture.ProjectId;
            string locationId = _fixture.LocationId;

            // Get TemplateName for testing
            TemplateName templateName = _fixture.CreateTemplateName();

            // Get template ID from TemplateName
            string templateId = templateName.TemplateId;

            // Build the inspect template name.
            // string inspectTemplateName = DeidentifyTemplateName
            //     .FormatProjectLocationDeidentifyTemplate(projectId, locationId, inspectTemplateId)
            //     .ToString();

            // Build the deidentify template name.
            // string deidentifyTemplateName = DeidentifyTemplateName
            //     .FormatProjectLocationDeidentifyTemplate(
            //         projectId,
            //         locationId,
            //         deidentifyTemplateName.DeidentifyTemplateId
            //     )
            //     .ToString();

            // Run the sample
            Template template = _sample.CreateTemplateWithAdvancedSdp(
                projectId: projectId,
                locationId: locationId,
                templateId: templateId,
                InspectTemplateId: InspectTemplateName.InspectTemplateId,
                deidentifyTemplateId: DeidentifyTemplateName.DeidentifyTemplateId
            );

            // Output template details
            _output.WriteLine($"Created template: {template.Name}");

            // Verify the template was created successfully
            Assert.NotNull(template);
            Assert.Contains(templateId, template.Name);

            // Verify the template has the expected filter configuration
            Assert.NotNull(template.FilterConfig);
            Assert.NotNull(template.FilterConfig.SdpSettings);
            Assert.NotNull(template.FilterConfig.SdpSettings.AdvancedConfig);

            // Verify the advanced SDP config
            var advancedConfig = template.FilterConfig.SdpSettings.AdvancedConfig;
            Assert.Contains(inspectTemplateId, advancedConfig.InspectTemplate);
            Assert.Contains(
                deidentifyTemplateName.DeidentifyTemplateId,
                advancedConfig.DeidentifyTemplate
            );

            // Verify the inspect and deidentify template names
            Assert.Equal(inspectTemplateName, advancedConfig.InspectTemplate);
            Assert.Equal(deidentifyTemplateName, advancedConfig.DeidentifyTemplate);
        }
    }
}
