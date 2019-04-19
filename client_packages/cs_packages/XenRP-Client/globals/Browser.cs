using System.Linq;
using RAGE;
using RAGE.Ui;

namespace XenRP.Client.globals {
    internal class Browser : Events.Script {
        private static object[] parameters;
        public static HtmlWindow customBrowser;

        public Browser() {
            Events.Add("createBrowser", CreateBrowserEvent);
            Events.Add("executeFunction", ExecuteFunctionEvent);
            Events.Add("destroyBrowser", DestroyBrowserEvent);
            Events.OnBrowserCreated += OnBrowserCreatedEvent;
        }

        public static void CreateBrowserEvent(object[] args) {
            if (customBrowser == null) {
                // Get the URL from the parameters
                var url = args[0].ToString();

                // Save the rest of the parameters
                parameters = args.Skip(1).ToArray();

                // Create the browser
                customBrowser = new HtmlWindow(url);
            }
        }

        public static void ExecuteFunctionEvent(object[] args) {
            // Check for the parameters
            var input = string.Empty;

            // Split the function and arguments
            var function = args[0].ToString();
            var arguments = args.Skip(1).ToArray();

            foreach (var arg in arguments)
                // Append all the arguments
                input += input.Length > 0 ? ", '" + arg + "'" : "'" + arg + "'";

            // Call the function with the parameters
            customBrowser.ExecuteJs(function + "(" + input + ");");
        }

        public static void DestroyBrowserEvent(object[] args) {
            // Disable the cursor
            Cursor.Visible = false;

            // Destroy the browser
            customBrowser.Destroy();
            customBrowser = null;
        }

        public static void OnBrowserCreatedEvent(HtmlWindow window) {
            if (window.Id == customBrowser.Id) {
                // Enable the cursor
                Cursor.Visible = true;

                if (parameters.Length > 0) ExecuteFunctionEvent(parameters);
            }
        }
    }
}