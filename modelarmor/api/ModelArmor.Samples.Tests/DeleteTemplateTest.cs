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
using System.Linq;
using Google.Cloud.ModelArmor.V1;
using Grpc.Core;
using Xunit;
using Xunit.Abstractions;

namespace ModelArmor.Samples.Tests
{
    public class DeleteTemplateTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly DeleteTemplateSample _sample;
        private readonly ListTemplatesSample _listSample;
        private readonly ITestOutputHelper _output;

        public DeleteTemplateTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new DeleteTemplateSample();
            _listSample = new ListTemplatesSample();
            _output = output;
        }

        [Fact]
        public void DeleteTemplateTest()
        {
            // Create a template to delete
            Template createdTemplate = _fixture.CreateBaseTemplate();

            // Extract the template ID from the resource name
            string templateId = TemplateName.Parse(createdTemplate.Name).TemplateId;
            string templateName = createdTemplate.Name;

            // Verify the template exists before deletion
            var templates = _listSample
                .ListTemplates(projectId: _fixture.ProjectId, locationId: _fixture.LocationId)
                .ToList();

            Assert.Contains(templates, t => t.Name == templateName);

            // Call the sample to delete the template
            _sample.DeleteTemplate(
                projectId: _fixture.ProjectId,
                locationId: _fixture.LocationId,
                templateId: templateId
            );

            // Verify the template no longer exists
            templates = _listSample
                .ListTemplates(projectId: _fixture.ProjectId, locationId: _fixture.LocationId)
                .ToList();

            Assert.DoesNotContain(templates, t => t.Name == templateName);

            // Try to get the template directly - should throw a "not found" exception
            var exception = Assert.Throws<RpcException>(() =>
            {
                var getTemplateSample = new GetTemplateSample();
                getTemplateSample.GetTemplate(
                    projectId: _fixture.ProjectId,
                    locationId: _fixture.LocationId,
                    templateId: templateId
                );
            });

            Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        }

        [Fact]
        public void DeleteTemplateTest_NonExistentTemplate()
        {
            // Generate a random template ID that shouldn't exist
            string nonExistentTemplateId =
                $"non-existent-{Guid.NewGuid().ToString("N").Substring(0, 8)}";

            // Attempt to delete a non-existent template should throw an exception
            var exception = Assert.Throws<RpcException>(() =>
                _sample.DeleteTemplate(
                    projectId: _fixture.ProjectId,
                    locationId: _fixture.LocationId,
                    templateId: nonExistentTemplateId
                )
            );

            // Verify the exception contains the expected error code
            Assert.Equal(StatusCode.NotFound, exception.StatusCode);
        }
    }
}
