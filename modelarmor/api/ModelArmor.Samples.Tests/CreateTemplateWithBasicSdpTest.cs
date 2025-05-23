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
using Google.Cloud.ModelArmor.V1;
using Xunit;
using Xunit.Abstractions;

namespace ModelArmor.Samples.Tests
{
    public class CreateTemplateWithBasicSdpTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly CreateTemplateWithBasicSdpSample _sample;
        private readonly ITestOutputHelper _output;

        public CreateTemplateWithBasicSdpTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new CreateTemplateWithBasicSdpSample();
            _output = output;
        }

        [Fact]
        public void CreateTemplateWithBasicSdpTest()
        {
            string projectId = _fixture.ProjectId;
            string locationId = _fixture.LocationId;

            // Get TemplateName for testing
            TemplateName templateName = _fixture.CreateTemplateName();

            // Get template ID from TemplateName
            string templateId = templateName.TemplateId;

            // Run the sample
            Template template = _sample.CreateTemplateWithBasicSdp(
                projectId: projectId,
                locationId: locationId,
                templateId: templateId
            );

            // Verify the template was created successfully
            Assert.NotNull(template);
            Assert.Contains(templateId, template.Name);

            // Verify the template has the expected filter configuration
            Assert.NotNull(template.FilterConfig);
            Assert.NotNull(template.FilterConfig.SdpSettings);
            Assert.NotNull(template.FilterConfig.SdpSettings.BasicConfig);

            // Verify the basic SDP config has enforcement enabled
            var basicConfig = template.FilterConfig.SdpSettings.BasicConfig;
            Assert.Equal(
                SdpBasicConfig.Types.SdpBasicConfigEnforcement.Enabled,
                basicConfig.FilterEnforcement
            );

            // Clean up - delete the template (if needed)
            try
            {
                _fixture.Client.DeleteTemplate(new DeleteTemplateRequest { Name = template.Name });
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Error during cleanup: {ex.Message}");
                // Don't fail the test if cleanup fails
            }
        }
    }
}
