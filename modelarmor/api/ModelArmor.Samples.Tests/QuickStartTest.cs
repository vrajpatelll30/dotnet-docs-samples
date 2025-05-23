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
    public class QuickstartTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly QuickstartSample _sample;
        private readonly ITestOutputHelper _output;

        public QuickstartTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new QuickstartSample();
            _output = output;
        }

        [Fact]
        public void QuickstartTest()
        {
            // Create a sample template name
            TemplateName templateName = _fixture.CreateTemplateName();

            // Extract templateId from the full resource name
            string templateId = templateName.TemplateId;

            // Run the quickstart sample
            _sample.Quickstart(
                projectId: _fixture.ProjectId,
                locationId: _fixture.LocationId,
                templateId: templateId
            );
        }
    }
}
