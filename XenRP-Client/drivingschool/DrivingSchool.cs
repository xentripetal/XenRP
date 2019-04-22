using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using RAGE;
using RAGE.Elements;
using XenRP.Client.globals;
using XenRP.Client.model;

namespace XenRP.Client.drivingschool {
    internal class DrivingSchool : Events.Script {
        private List<DrivingTest> answersList;
        private Blip licenseBlip;
        private List<DrivingTest> questionsList;

        public DrivingSchool() {
            Events.Add("startLicenseExam", StartLicenseExamEvent);
            Events.Add("getNextTestQuestion", GetNextTestQuestionEvent);
            Events.Add("submitAnswer", SubmitAnswerEvent);
            Events.Add("finishLicenseExam", FinishLicenseExamEvent);
            Events.Add("showLicenseCheckpoint", ShowLicenseCheckpointEvent);
            Events.Add("deleteLicenseCheckpoint", DeleteLicenseCheckpointEvent);
        }

        private void StartLicenseExamEvent(object[] args) {
            // Get the variables from the arguments
            var questionsJson = args[0].ToString();
            var answersJson = args[1].ToString();

            // Get the exam questions and answers
            questionsList = JsonConvert.DeserializeObject<List<DrivingTest>>(questionsJson);
            answersList = JsonConvert.DeserializeObject<List<DrivingTest>>(answersJson);

            // Disable the chat
            Chat.Activate(false);
            Chat.Show(false);

            // Show the question
            Browser.CreateBrowserEvent(new object[]
                {"package://statics/html/licenseExam.html", "getFirstTestQuestion"});
        }

        private void GetNextTestQuestionEvent(object[] args) {
            // Get the current question number
            var index = (int) Player.LocalPlayer.GetSharedData("PLAYER_LICENSE_QUESTION");

            // Load the question and initialize the answers
            var questionText = questionsList[index].text;

            var answers = answersList.Where(test => test.question == questionsList[index].id).ToList();
            var answersJson = JsonConvert.SerializeObject(answers);

            // Show the question into the browser
            Browser.ExecuteFunctionEvent(new object[] {"populateQuestionAnswers", questionText, answersJson});
        }

        private void SubmitAnswerEvent(object[] args) {
            // Get the variables from the arguments
            var answer = Convert.ToInt32(args[0]);

            // Check if the answer is correct
            Events.CallRemote("checkAnswer", answer);
        }

        private void FinishLicenseExamEvent(object[] args) {
            // Enable the chat
            Chat.Activate(true);
            Chat.Show(true);

            // Destroy the exam's window
            Browser.DestroyBrowserEvent(null);
        }

        private void ShowLicenseCheckpointEvent(object[] args) {
            // Get the variables from the arguments
            var position = (Vector3) args[0];

            if (licenseBlip == null)
                licenseBlip = new Blip(1, position, string.Empty, 1, 1);
            else
                licenseBlip.SetCoords(position.X, position.Y, position.Z);
        }

        private void DeleteLicenseCheckpointEvent(object[] args) {
            // Destroy the blip on the map
            licenseBlip.Destroy();
            licenseBlip = null;
        }
    }
}