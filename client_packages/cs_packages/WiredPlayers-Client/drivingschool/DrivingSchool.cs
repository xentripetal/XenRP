using RAGE;
using RAGE.Elements;
using Newtonsoft.Json;
using WiredPlayers_Client.model;
using WiredPlayers_Client.globals;
using System.Collections.Generic;
using System.Linq;

namespace WiredPlayers_Client.drivingschool
{
    class DrivingSchool : Events.Script
    {
        private Blip licenseBlip = null;
        private List<DrivingTest> questionsList;
        private List<DrivingTest> answersList;

        public DrivingSchool()
        {
            Events.Add("startLicenseExam", StartLicenseExamEvent);
            Events.Add("getNextTestQuestion", GetNextTestQuestionEvent);
            Events.Add("submitAnswer", SubmitAnswerEvent);
            Events.Add("finishLicenseExam", FinishLicenseExamEvent);
            Events.Add("showLicenseCheckpoint", ShowLicenseCheckpointEvent);
            Events.Add("deleteLicenseCheckpoint", DeleteLicenseCheckpointEvent);
        }

        private void StartLicenseExamEvent(object[] args)
        {
            // Get the variables from the arguments
            string questionsJson = args[0].ToString();
            string answersJson = args[1].ToString();

            // Get the exam questions and answers
            questionsList = JsonConvert.DeserializeObject<List<DrivingTest>>(questionsJson);
            answersList = JsonConvert.DeserializeObject<List<DrivingTest>>(answersJson);

            // Disable the chat
            Chat.Activate(false);
            Chat.Show(false);

            // Show the question
            Browser.CreateBrowserEvent(new object[] { "package://statics/html/licenseExam.html", "getFirstTestQuestion" });
        }

        private void GetNextTestQuestionEvent(object[] args)
        {
            // Get the current question number
            int index = (int)Player.LocalPlayer.GetSharedData("PLAYER_LICENSE_QUESTION");

            // Load the question and initialize the answers
            string questionText = questionsList[index].text;

            List<DrivingTest> answers = answersList.Where(test => test.question == questionsList[index].id).ToList();
            string answersJson = JsonConvert.SerializeObject(answers);

            // Show the question into the browser
            Browser.ExecuteFunctionEvent(new object[] { "populateQuestionAnswers", questionText, answersJson });
        }

        private void SubmitAnswerEvent(object[] args)
        {
            // Check if the answer is correct
            Events.CallRemote("checkAnswer", (int)args[0]);
        }

        private void FinishLicenseExamEvent(object[] args)
        {
            // Enable the chat
            Chat.Activate(true);
            Chat.Show(true);

            // Destroy the exam's window
            Browser.DestroyBrowserEvent(null);
        }

        private void ShowLicenseCheckpointEvent(object[] args)
        {
            // Get the variables from the arguments
            Vector3 position = (Vector3)args[0];

            if (licenseBlip == null)
            {
                // Create a blip on the map
                licenseBlip = new Blip(1, position, string.Empty, 1, 1);
            }
            else
            {
                // Update blip's position
                licenseBlip.SetCoords(position.X, position.Y, position.Z);
            }
        }

        private void DeleteLicenseCheckpointEvent(object[] args)
        {
            // Destroy the blip on the map
            licenseBlip.Destroy();
            licenseBlip = null;
        }
    }
}
