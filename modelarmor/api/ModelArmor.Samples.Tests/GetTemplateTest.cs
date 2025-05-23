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
    public class GetTemplateTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly GetTemplateSample _sample;
        private readonly ITestOutputHelper _output;

        public GetTemplateTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new GetTemplateSample();
            _output = output;
        }

        [Fact]
        public void GetTemplateTest()
        {
            // Create a sample template name
            Template createdTemplate = _fixture.CreateBaseTemplate();

            // Extract templateId from the full resource name
            string templateId = TemplateName.Parse(createdTemplate.Name).TemplateId;

            // Run the get Template
            Template recievedTemplate = _sample.GetTemplate(
                projectId: _fixture.ProjectId,
                locationId: _fixture.LocationId,
                templateId: templateId
            );

            var revievedRaiFilters = recievedTemplate.FilterConfig.RaiSettings.RaiFilters;

            // Verify the revieved filter settings match what we expect
            Assert.Contains(
                revievedRaiFilters,
                f =>
                    f.FilterType == RaiFilterType.Dangerous
                    && f.ConfidenceLevel == DetectionConfidenceLevel.High
            );

            Assert.Contains(
                revievedRaiFilters,
                f =>
                    f.FilterType == RaiFilterType.HateSpeech
                    && f.ConfidenceLevel == DetectionConfidenceLevel.MediumAndAbove
            );

            Assert.Contains(
                revievedRaiFilters,
                f =>
                    f.FilterType == RaiFilterType.SexuallyExplicit
                    && f.ConfidenceLevel == DetectionConfidenceLevel.MediumAndAbove
            );

            Assert.Contains(
                revievedRaiFilters,
                f =>
                    f.FilterType == RaiFilterType.Harassment
                    && f.ConfidenceLevel == DetectionConfidenceLevel.MediumAndAbove
            );
        }
    }
}
