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
using Google.Cloud.ModelArmor.V1;
using Xunit;
using Xunit.Abstractions;

namespace ModelArmor.Samples.Tests
{
    public class ListTemplatesTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly ListTemplatesSample _sample;
        private readonly ITestOutputHelper _output;

        public ListTemplatesTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new ListTemplatesSample();
            _output = output;
        }

        [Fact]
        public void ListTemplatesTest()
        {
            // Create a template to ensure there's at least one to list
            Template createdTemplate = _fixture.CreateBaseTemplate();

            // Extract the template ID from the resource name
            string templateId = TemplateName.Parse(createdTemplate.Name).TemplateId;
            string expectedTemplateName = createdTemplate.Name;

            // Call the sample to list all templates
            IEnumerable<Template> templates = _sample.ListTemplates(
                projectId: _fixture.ProjectId,
                locationId: _fixture.LocationId
            );

            // Verify we got some templates back
            Assert.NotNull(templates);

            // Convert to list to enable multiple iterations and get count
            List<Template> templateList = templates.ToList();

            // Verify our created template is in the list
            bool templateFound = false;
            foreach (Template template in templateList)
            {
                if (template.Name == expectedTemplateName)
                {
                    templateFound = true;
                    break;
                }
            }

            // Assert that our template was found in the list
            Assert.True(
                templateFound,
                $"Created template {expectedTemplateName} was not found in the list of templates"
            );
        }
    }
}
