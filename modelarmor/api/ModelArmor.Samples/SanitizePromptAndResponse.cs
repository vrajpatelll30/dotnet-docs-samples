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

// [START modelarmor_sanitize_prompt_and_response]
using System;
using System.Collections.Generic;
using Google.Api.Gax.ResourceNames;
using Google.Cloud.ModelArmor.V1;
using Newtonsoft.Json;

namespace ModelArmor.Samples
{
    public class SanitizePromptAndResponseSample
    {
        /// <summary>
        /// Sanitizes both user prompt and model response using the same template configuration.
        /// </summary>
        /// <param name="projectId">Google Cloud project ID</param>
        /// <param name="locationId">Location ID (e.g., us-central1)</param>
        /// <param name="templateId">Template ID for sanitization</param>
        /// <param name="userPrompt">User prompt to sanitize</param>
        /// <param name="modelResponse">Model response to sanitize</param>
        /// <returns>A dictionary containing both sanitization results</returns>
        public Dictionary<string, object> SanitizePromptAndResponse(
            string projectId = "my-project",
            string locationId = "us-central1",
            string templateId = "my-template",
            string userPrompt = "Unsafe user prompt",
            string modelResponse = "Unsanitized model output"
        )
        {
            // Endpoint to call the Model Armor server.
            ModelArmorClientBuilder clientBuilder = new ModelArmorClientBuilder
            {
                Endpoint = $"modelarmor.{locationId}.rep.googleapis.com",
            };

            // Create the client.
            ModelArmorClient client = clientBuilder.Build();

            // Build the resource name of the template.
            TemplateName templateName = TemplateName.FromProjectLocationTemplate(
                projectId,
                locationId,
                templateId
            );

            // Prepare and send the user prompt sanitization request
            SanitizeUserPromptRequest promptRequest = new SanitizeUserPromptRequest
            {
                TemplateName = templateName,
                UserPromptData = new DataItem { Text = userPrompt },
            };
            SanitizeUserPromptResponse promptResponse = client.SanitizeUserPrompt(promptRequest);

            // Prepare and send the model response sanitization request
            SanitizeModelResponseRequest responseRequest = new SanitizeModelResponseRequest
            {
                TemplateName = templateName,
                ModelResponseData = new DataItem { Text = modelResponse },
            };
            SanitizeModelResponseResponse responseResult = client.SanitizeModelResponse(
                responseRequest
            );

            // Check if the user prompt contains harmful content
            bool isPromptSafe =
                promptResponse.SanitizationResult.FilterMatchState == FilterMatchState.NoMatchFound;

            // Check if the model response contains harmful content
            bool isResponseSafe =
                responseResult.SanitizationResult.FilterMatchState == FilterMatchState.NoMatchFound;

            // Print sanitization results
            Console.WriteLine(
                $"User prompt sanitization result: {(isPromptSafe ? "Safe" : "Unsafe")}"
            );
            Console.WriteLine(
                $"Model response sanitization result: {(isResponseSafe ? "Safe" : "Unsafe")}"
            );

            // Return both results in a dictionary
            return new Dictionary<string, object>
            {
                { "promptSanitizationResult", promptResponse },
                { "responseSanitizationResult", responseResult },
                { "isPromptSafe", isPromptSafe },
                { "isResponseSafe", isResponseSafe },
            };
        }

        /// <summary>
        /// Sanitizes both user prompt and model response with detailed handling of SDP findings.
        /// </summary>
        /// <param name="projectId">Google Cloud project ID</param>
        /// <param name="locationId">Location ID (e.g., us-central1)</param>
        /// <param name="templateId">Template ID for sanitization</param>
        /// <param name="userPrompt">User prompt to sanitize</param>
        /// <param name="modelResponse">Model response to sanitize</param>
        /// <returns>A dictionary containing both sanitization results and deidentified content</returns>
        public Dictionary<string, object> SanitizePromptAndResponseWithSdp(
            string projectId = "my-project",
            string locationId = "us-central1",
            string templateId = "my-template",
            string userPrompt = "My email is user@example.com and my phone is 555-123-4567",
            string modelResponse = "I found your ITIN: 988-86-1234 in our records"
        )
        {
            // Endpoint to call the Model Armor server.
            ModelArmorClientBuilder clientBuilder = new ModelArmorClientBuilder
            {
                Endpoint = $"modelarmor.{locationId}.rep.googleapis.com",
            };

            // Create the client.
            ModelArmorClient client = clientBuilder.Build();

            // Build the resource name of the template.
            TemplateName templateName = TemplateName.FromProjectLocationTemplate(
                projectId,
                locationId,
                templateId
            );

            // Prepare and send the user prompt sanitization request
            SanitizeUserPromptRequest promptRequest = new SanitizeUserPromptRequest
            {
                TemplateName = templateName,
                UserPromptData = new DataItem { Text = userPrompt },
            };
            SanitizeUserPromptResponse promptResponse = client.SanitizeUserPrompt(promptRequest);

            // Prepare and send the model response sanitization request
            SanitizeModelResponseRequest responseRequest = new SanitizeModelResponseRequest
            {
                TemplateName = templateName,
                ModelResponseData = new DataItem { Text = modelResponse },
            };
            SanitizeModelResponseResponse modelResponseResult = client.SanitizeModelResponse(
                responseRequest
            );

            _out
            
            // Extract deidentified content if available
            string deidentifiedPrompt = userPrompt;
            string deidentifiedResponse = modelResponse;

            // Process user prompt SDP results
            if (promptResponse.SanitizationResult.FilterResults.ContainsKey("sdp"))
            {
                var sdpResult = promptResponse.SanitizationResult.FilterResults["sdp"];
                if (sdpResult.SdpFilterResult?.DeidentifyResult != null)
                {
                    deidentifiedPrompt = sdpResult.SdpFilterResult.DeidentifyResult.Data.Text;
                }
            }

            // Process model response SDP results
            if (modelResponseResult.SanitizationResult.FilterResults.ContainsKey("sdp"))
            {
                var sdpResult = modelResponseResult.SanitizationResult.FilterResults["sdp"];
                if (sdpResult.SdpFilterResult?.DeidentifyResult != null)
                {
                    deidentifiedResponse = sdpResult.SdpFilterResult.DeidentifyResult.Data.Text;
                }
            }

            // Print sanitization results
            Console.WriteLine($"Original user prompt: {userPrompt}");
            Console.WriteLine($"Deidentified user prompt: {deidentifiedPrompt}");
            Console.WriteLine($"Original model response: {modelResponse}");
            Console.WriteLine($"Deidentified model response: {deidentifiedResponse}");

            // Return results in a dictionary
            return new Dictionary<string, object>
            {
                { "promptSanitizationResult", promptResponse },
                { "responseSanitizationResult", modelResponseResult },
                { "deidentifiedPrompt", deidentifiedPrompt },
                { "deidentifiedResponse", deidentifiedResponse },
            };
        }
    }
}
// [END modelarmor_sanitize_prompt_and_response]
