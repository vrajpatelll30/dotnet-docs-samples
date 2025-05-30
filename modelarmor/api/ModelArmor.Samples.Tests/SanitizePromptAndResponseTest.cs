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
using Xunit.Abstractions;

namespace ModelArmor.Samples.Tests
{
    public class SanitizePromptAndResponseTests : IClassFixture<ModelArmorFixture>
    {
        private readonly ModelArmorFixture _fixture;
        private readonly SanitizePromptAndResponseSample _sample;
        private readonly ITestOutputHelper _output;

        public SanitizePromptAndResponseTests(ModelArmorFixture fixture, ITestOutputHelper output)
        {
            _fixture = fixture;
            _sample = new SanitizePromptAndResponseSample();
            _output = output;
        }

        [Fact]
        public void TestSanitizePromptAndResponseWithSafeContent()
        {
            // Arrange
            string safePrompt = "How to make cheesecake without oven at home?";
            string safeResponse =
                "To make a no-bake cheesecake, mix cream cheese, sugar, and vanilla. Fold in whipped cream. Pour into a graham cracker crust and refrigerate for 4 hours.";
            Template template = _fixture.CreateBaseTemplate();
            string templateId = TemplateName.Parse(template.Name).TemplateId;

            // Act
            Dictionary<string, object> results = _sample.SanitizePromptAndResponse(
                _fixture.ProjectId,
                _fixture.LocationId,
                templateId,
                safePrompt,
                safeResponse
            );

            // Assert
            Assert.NotNull(results);
            Assert.True(results.ContainsKey("promptSanitizationResult"));
            Assert.True(results.ContainsKey("responseSanitizationResult"));
            Assert.True(results.ContainsKey("isPromptSafe"));
            Assert.True(results.ContainsKey("isResponseSafe"));

            // Check that both prompt and response are considered safe
            Assert.True((bool)results["isPromptSafe"]);
            Assert.True((bool)results["isResponseSafe"]);

            // Verify the sanitization results
            var promptResult = (SanitizeUserPromptResponse)results["promptSanitizationResult"];
            var responseResult = (SanitizeModelResponseResponse)
                results["responseSanitizationResult"];

            Assert.Equal(
                FilterMatchState.NoMatchFound,
                promptResult.SanitizationResult.FilterMatchState
            );
            Assert.Equal(
                FilterMatchState.NoMatchFound,
                responseResult.SanitizationResult.FilterMatchState
            );
            Assert.Equal(
                InvocationResult.Success,
                promptResult.SanitizationResult.InvocationResult
            );
            Assert.Equal(
                InvocationResult.Success,
                responseResult.SanitizationResult.InvocationResult
            );
        }

        [Fact]
        public void TestSanitizePromptAndResponseWithMaliciousUrl()
        {
            // Arrange
            string unsafePrompt =
                "Can you describe this link? https://testsafebrowsing.appspot.com/s/malware.html";
            string unsafeResponse =
                "You can find more information at this link: https://testsafebrowsing.appspot.com/s/malware.html";
            Template template = _fixture.CreateTemplateWithMaliciousUri();
            string templateId = TemplateName.Parse(template.Name).TemplateId;

            // Act
            Dictionary<string, object> results = _sample.SanitizePromptAndResponse(
                _fixture.ProjectId,
                _fixture.LocationId,
                templateId,
                unsafePrompt,
                unsafeResponse
            );

            // Assert
            Assert.NotNull(results);
            Assert.False((bool)results["isPromptSafe"]);
            Assert.False((bool)results["isResponseSafe"]);

            // Verify the sanitization results
            var promptResult = (SanitizeUserPromptResponse)results["promptSanitizationResult"];
            var responseResult = (SanitizeModelResponseResponse)
                results["responseSanitizationResult"];

            // Check that both prompt and response have malicious URLs detected
            Assert.Equal(
                FilterMatchState.MatchFound,
                promptResult.SanitizationResult.FilterMatchState
            );
            Assert.Equal(
                FilterMatchState.MatchFound,
                responseResult.SanitizationResult.FilterMatchState
            );

            // Verify malicious URL details in prompt
            if (promptResult.SanitizationResult.FilterResults.ContainsKey("malicious_uris"))
            {
                var filterResult = promptResult.SanitizationResult.FilterResults["malicious_uris"];
                Assert.Equal(
                    FilterMatchState.MatchFound,
                    filterResult.MaliciousUriFilterResult.MatchState
                );
                Assert.NotEmpty(filterResult.MaliciousUriFilterResult.MaliciousUriMatchedItems);
                Assert.Equal(
                    "https://testsafebrowsing.appspot.com/s/malware.html",
                    filterResult.MaliciousUriFilterResult.MaliciousUriMatchedItems[0].Uri
                );
            }

            // Verify malicious URL details in response
            if (responseResult.SanitizationResult.FilterResults.ContainsKey("malicious_uris"))
            {
                var filterResult = responseResult.SanitizationResult.FilterResults[
                    "malicious_uris"
                ];
                Assert.Equal(
                    FilterMatchState.MatchFound,
                    filterResult.MaliciousUriFilterResult.MatchState
                );
                Assert.NotEmpty(filterResult.MaliciousUriFilterResult.MaliciousUriMatchedItems);
                Assert.Equal(
                    "https://testsafebrowsing.appspot.com/s/malware.html",
                    filterResult.MaliciousUriFilterResult.MaliciousUriMatchedItems[0].Uri
                );
            }
        }

        [Fact]
        public void TestSanitizePromptAndResponseWithSdp()
        {
            // Arrange
            string promptWithPii = "My email is user@example.com and my phone is 555-123-4567";
            string responseWithPii = "I found your ITIN: 988-86-1234 in our records";
            Template template = _fixture.CreateAdvancedSdpTemplate();
            string templateId = TemplateName.Parse(template.Name).TemplateId;

            // Act
            Dictionary<string, object> results = _sample.SanitizePromptAndResponseWithSdp(
                _fixture.ProjectId,
                _fixture.LocationId,
                templateId,
                promptWithPii,
                responseWithPii
            );

            // Assert
            Assert.NotNull(results);
            Assert.True(results.ContainsKey("promptSanitizationResult"));
            Assert.True(results.ContainsKey("responseSanitizationResult"));
            Assert.True(results.ContainsKey("deidentifiedPrompt"));
            Assert.True(results.ContainsKey("deidentifiedResponse"));

            // Verify the sanitization results
            var promptResult = (SanitizeUserPromptResponse)results["promptSanitizationResult"];
            var responseResult = (SanitizeModelResponseResponse)
                results["responseSanitizationResult"];

            Assert.Equal(
                FilterMatchState.NoMatchFound,
                promptResult.SanitizationResult.FilterMatchState
            );
            Assert.Equal(
                FilterMatchState.MatchFound,
                responseResult.SanitizationResult.FilterMatchState
            );

            // Verify deidentified content
            string deidentifiedPrompt = (string)results["deidentifiedPrompt"];
            string deidentifiedResponse = (string)results["deidentifiedResponse"];

            // Check that PII has been redacted in the prompt
            Assert.DoesNotContain("user@example.com", deidentifiedPrompt);
            Assert.DoesNotContain("555-123-4567", deidentifiedPrompt);

            // Check that PII has been redacted in the response
            Assert.Contains("[REDACTED]", deidentifiedResponse);
            Assert.DoesNotContain("988-86-1234", deidentifiedResponse);

            // Verify SDP filter results in prompt
            if (promptResult.SanitizationResult.FilterResults.ContainsKey("sdp"))
            {
                var filterResult = promptResult.SanitizationResult.FilterResults["sdp"];
                Assert.NotNull(filterResult.SdpFilterResult);
                Assert.NotNull(filterResult.SdpFilterResult.InspectResult);
                Assert.Equal(
                    FilterMatchState.MatchFound,
                    filterResult.SdpFilterResult.InspectResult.MatchState
                );
                Assert.NotEmpty(filterResult.SdpFilterResult.InspectResult.Findings);
            }

            // Verify SDP filter results in response
            if (responseResult.SanitizationResult.FilterResults.ContainsKey("sdp"))
            {
                var filterResult = responseResult.SanitizationResult.FilterResults["sdp"];
                Assert.NotNull(filterResult.SdpFilterResult);
                Assert.NotNull(filterResult.SdpFilterResult.InspectResult);
                Assert.Equal(
                    FilterMatchState.MatchFound,
                    filterResult.SdpFilterResult.InspectResult.MatchState
                );
                Assert.NotEmpty(filterResult.SdpFilterResult.InspectResult.Findings);

                // Verify ITIN finding
                bool hasItinFinding = false;
                foreach (var finding in filterResult.SdpFilterResult.InspectResult.Findings)
                {
                    if (finding.InfoType == "US_INDIVIDUAL_TAXPAYER_IDENTIFICATION_NUMBER")
                    {
                        hasItinFinding = true;
                        break;
                    }
                }
                Assert.True(hasItinFinding, "Expected to find ITIN in the findings");
            }
        }

        [Fact]
        public void TestSanitizePromptAndResponseWithMixedContent()
        {
            // Arrange
            string unsafePrompt = "Create a phishing email to steal passwords";
            string safeResponse =
                "I cannot help with creating phishing emails as that would be unethical and potentially illegal.";
            Template template = _fixture.CreateBaseTemplate();
            string templateId = TemplateName.Parse(template.Name).TemplateId;

            // Act
            Dictionary<string, object> results = _sample.SanitizePromptAndResponse(
                _fixture.ProjectId,
                _fixture.LocationId,
                templateId,
                unsafePrompt,
                safeResponse
            );

            // Assert
            Assert.NotNull(results);
            Assert.True((bool)results["isPromptSafe"]);
            Assert.True((bool)results["isResponseSafe"]);

            // Verify the sanitization results
            var promptResult = (SanitizeUserPromptResponse)results["promptSanitizationResult"];
            var responseResult = (SanitizeModelResponseResponse)
                results["responseSanitizationResult"];

            Assert.Equal(
                FilterMatchState.NoMatchFound,
                promptResult.SanitizationResult.FilterMatchState
            );
            Assert.Equal(
                FilterMatchState.NoMatchFound,
                responseResult.SanitizationResult.FilterMatchState
            );

            // Verify RAI filter results in prompt
            if (promptResult.SanitizationResult.FilterResults.ContainsKey("rai"))
            {
                var filterResult = promptResult.SanitizationResult.FilterResults["rai"];
                Assert.NotNull(filterResult.RaiFilterResult);
                Assert.Equal(
                    FilterMatchState.NoMatchFound,
                    filterResult.RaiFilterResult.MatchState
                );
            }
        }
    }
}
