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

// [START modelarmor_update_template_with_labels]
using System;
using Google.Cloud.ModelArmor.V1;
using Google.Protobuf.WellKnownTypes;

namespace ModelArmor.Samples
{
    public class UpdateTemplateWithLabelsSample
    {
        public Template UpdateTemplateWithLabels(
            string projectId = "my-project",
            string locationId = "us-central1",
            string templateId = "my-template"
        )
        {
            // Construct the API endpoint URL.
            ModelArmorClientBuilder clientBuilder = new ModelArmorClientBuilder
            {
                Endpoint = $"modelarmor.{locationId}.rep.googleapis.com",
            };

            // Create the client.
            ModelArmorClient client = clientBuilder.Build();

            // Get the template name.
            TemplateName name = TemplateName.FromProjectLocationTemplate(
                projectId,
                locationId,
                templateId
            );

            // Create the template with updated filter config
            Template template = new Template { TemplateName = name };

            template.Labels.Add("key1", "updatedvalue1");
            template.Labels.Add("key2", "updatedvalue2");

            // Create a field mask to specify which fields to update.
            // Ref: https://protobuf.dev/reference/protobuf/google.protobuf/#field-mask
            FieldMask updateMask = new FieldMask { Paths = { "labels" } };

            // Create the update request
            UpdateTemplateRequest request = new UpdateTemplateRequest
            {
                Template = template,
                UpdateMask = updateMask,
            };

            // Send the request
            Template updatedTemplate = client.UpdateTemplate(request);
            Console.WriteLine($"Updated template: {updatedTemplate.Name}");

            return updatedTemplate;
        }
    }
}
// [END modelarmor_update_template_with_labels]
