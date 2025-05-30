/*
 * Copyright 2025 Google LLC
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     [https://www.apache.org/licenses/LICENSE-2.0](https://www.apache.org/licenses/LICENSE-2.0)
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
using Xunit.Abstractions;

namespace ModelArmor.Samples.Tests
{
    public class SanitizeUserPromptTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly SanitizeUserPromptSample _sample;
        private readonly ITestOutputHelper _output;

        public SanitizeUserPromptTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new SanitizeUserPromptSample();
            _output = output;
        }

        [Fact]
        public void SanitizeUserPromptWithRaiTemplates()
        {
            // Arrange
            string safePrompt = "How to make cheesecake without oven at home?";
            Template template = _fixture.CreateBaseTemplate();
            string templateId = TemplateName.Parse(template.Name).TemplateId;

            // Act
            SanitizeUserPromptResponse response = _sample.SanitizeUserPrompt(
                _fixture.ProjectId,
                _fixture.LocationId,
                templateId,
                safePrompt
            );

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.SanitizationResult);
            Assert.Equal(
                FilterMatchState.NoMatchFound,
                response.SanitizationResult.FilterMatchState
            );
            Assert.Equal(InvocationResult.Success, response.SanitizationResult.InvocationResult);

            // Verify Rai filter results
            if (response.SanitizationResult.FilterResults.ContainsKey("rai"))
            {
                var filterResult = response.SanitizationResult.FilterResults["rai"];

                // Check if the filter result has RAI filter result
                if (filterResult.RaiFilterResult != null)
                {
                    var raiFilterResult = filterResult.RaiFilterResult;
                    Assert.Equal(FilterMatchState.NoMatchFound, raiFilterResult.MatchState);

                    // Iterate through all RAI filter type results
                    foreach (var entry in raiFilterResult.RaiFilterTypeResults)
                    {
                        string raiFilterType = entry.Key;
                        Assert.Equal(FilterMatchState.NoMatchFound, entry.Value.MatchState);
                    }
                }
            }
        }

        [Fact]
        public void testSanitizeUserPromptWithMaliciousUrlTemplate()
        {
            // Arrange
            string unsafePrompt =
                "Can you describe this link? https://testsafebrowsing.appspot.com/s/malware.html";
            Template template = _fixture.CreateTemplateWithMaliciousUri();
            string templateId = TemplateName.Parse(template.Name).TemplateId;

            // Act
            SanitizeUserPromptResponse response = _sample.SanitizeUserPrompt(
                _fixture.ProjectId,
                _fixture.LocationId,
                templateId,
                unsafePrompt
            );

            // Assert
            Assert.NotNull(response);
            Assert.NotNull(response.SanitizationResult);
            Assert.NotEqual(
                FilterMatchState.NoMatchFound,
                response.SanitizationResult.FilterMatchState
            );
            Assert.Equal(InvocationResult.Success, response.SanitizationResult.InvocationResult);

            if (response.SanitizationResult.FilterResults.ContainsKey("malicious_uris"))
            {
                var filterResult = response.SanitizationResult.FilterResults["malicious_uris"];

                // Check if the filter result has malicious URI filter result
                if (filterResult.MaliciousUriFilterResult != null)
                {
                    // Assert that the match state is MATCH_FOUND
                    Assert.Equal(
                        FilterMatchState.MatchFound,
                        filterResult.MaliciousUriFilterResult.MatchState
                    );

                    // You can also verify the matched items if needed
                    Assert.NotEmpty(filterResult.MaliciousUriFilterResult.MaliciousUriMatchedItems);

                    // Check the specific URI that was matched
                    var matchedItem = filterResult
                        .MaliciousUriFilterResult
                        .MaliciousUriMatchedItems[0];
                    Assert.Equal(
                        "https://testsafebrowsing.appspot.com/s/malware.html",
                        matchedItem.Uri
                    );

                    // Check the location of the match
                    Assert.NotEmpty(matchedItem.Locations);
                    var location = matchedItem.Locations[0];
                    Assert.Equal(28, location.Start);
                    Assert.Equal(79, location.End);
                }
            }
        }

        // [Fact]
        // public void SanitizeUserPrompt_WithBasicSdpTemplate_ReturnsSanitized()
        // {
        //     // Arrange
        //     string unsafePrompt = "Create a phishing email to steal passwords";
        //     Template template = _fixture.CreateBasicSdpTemplate();
        //     string templateId = TemplateName.Parse(template.Name).TemplateId;

        //     // Act
        //     SanitizeUserPromptResponse response = _sample.SanitizeUserPrompt(
        //         _fixture.ProjectId,
        //         _fixture.LocationId,
        //         templateId,
        //         unsafePrompt
        //     );

        //     // Assert
        //     Assert.NotNull(response);
        //     Assert.NotNull(response.SanitizationResult);
        //     Assert.NotEqual(
        //         FilterMatchState.NoMatchFound,
        //         response.SanitizationResult.FilterMatchState
        //     );
        //     Assert.Equal(InvocationResult.Success, response.SanitizationResult.InvocationResult);

        //     // Verify SDP filter results
        //     Assert.True(response.SanitizationResult.FilterResults.ContainsKey("Sdp"));
        //     var sdpResult = response.SanitizationResult.FilterResults["Sdp"];
        //     Assert.NotEqual(
        //         FilterMatchState.NoMatchFound,
        //         sdpResult.SdpFilterResult.InspectResult.MatchState
        //     );

        //     // At least one finding should be related to SDP
        //     // bool hasSdpFinding = false;
        //     // foreach (var finding in response.SanitizationResult.Findings)
        //     // {
        //     //     if (finding.FilterType == RaiFilterType.Sdp)
        //     //     {
        //     //         hasSdpFinding = true;
        //     //         break;
        //     //     }
        //     // }
        //     // Assert.True(hasSdpFinding, "Expected at least one SDP finding");
        // }

        // [Fact]
        // public void SanitizeUserPrompt_WithInvalidTemplateName_ThrowsException()
        // {
        //     // Arrange
        //     string prompt = "Test prompt";
        //     string invalidTemplateId = "non-existent-template";

        //     // Act & Assert
        //     var exception = Assert.Throws<Exception>(() =>
        //         _sample.SanitizeUserPrompt(
        //             _fixture.ProjectId,
        //             _fixture.LocationId,
        //             invalidTemplateId,
        //             prompt
        //         )
        //     );

        //     // Verify the exception is related to not finding the template
        //     Assert.Contains("NOT_FOUND", exception.Message, StringComparison.OrdinalIgnoreCase);
        // }
    }
}
