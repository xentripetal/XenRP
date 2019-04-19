using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;
using XenRP.Client.model;

namespace XenRP.Client.account {
    internal class Register : Events.Script {
        public Register() {
            Events.Add("showRegisterWindow", ShowRegisterWindowEvent);
            Events.Add("showApplicationTest", ShowApplicationTestEvent);
            Events.Add("submitApplication", SubmitApplicationEvent);
            Events.Add("failedApplication", FailedApplicationEvent);
            Events.Add("retryApplication", RetryApplicationEvent);
            Events.Add("clearApplication", ClearApplicationEvent);
            Events.Add("createPlayerAccount", CreatePlayerAccountEvent);
        }

        private void ShowRegisterWindowEvent(object[] args) {
            // Create register window
            Browser.CreateBrowserEvent(new object[] {"package://statics/html/register.html"});
        }

        private void ShowApplicationTestEvent(object[] args) {
            // Destroy the current window
            Browser.DestroyBrowserEvent(null);

            // Create the application window
            Browser.CreateBrowserEvent(new object[] {
                "package://statics/html/application.html", "initializeApplication", args[0].ToString(),
                args[1].ToString()
            });
        }

        private void SubmitApplicationEvent(object[] args) {
            // Get the answers
            var questionsAnswers = new Dictionary<int, int>();
            var answers = JsonConvert.DeserializeObject<List<TestModel>>(args[0].ToString());

            foreach (var testModel in answers)
                // Add the question and answer to the dictionary
                questionsAnswers.Add(testModel.question, testModel.answer);

            // Send the answers to the server
            Events.CallRemote("submitApplication", JsonConvert.SerializeObject(questionsAnswers));
        }

        private void FailedApplicationEvent(object[] args) {
            // Get the mistakes
            var mistakes = Convert.ToInt32(args[0]);

            // Show the mistakes
            Browser.ExecuteFunctionEvent(new object[] {"showApplicationMistakes", mistakes});
        }

        private void RetryApplicationEvent(object[] args) {
            // Create a new application form
            Events.CallRemote("loadApplication");
        }

        private void ClearApplicationEvent(object[] args) {
            // Unfreeze the player
            Player.LocalPlayer.FreezePosition(false);

            // Show the message on the panel
            Browser.DestroyBrowserEvent(null);
        }

        private void CreatePlayerAccountEvent(object[] args) {
            // Get the password from the array
            var password = args[0].ToString();

            // Create login window
            Events.CallRemote("registerAccount", password);
        }
    }
}