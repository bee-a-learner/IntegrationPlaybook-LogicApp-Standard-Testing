using IPB.LogicApp.Standard.Testing.Helpers;
using IPB.LogicApp.Standard.Testing.Model;
using IPB.LogicApp.Standard.Testing.Model.WorkflowRunActionDetails;
using IPB.LogicApp.Standard.Testing.Model.WorkflowRunOverview;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Text;
using System.Threading;

namespace IPB.LogicApp.Standard.Testing
{
    public class LogicAppTestManager
    {
        private AuthenticationHelper _authHelper;
        private ManagementApiHelper _managementApiHelper;
        private WorkflowHelper _workflowHelper;
        private LogicAppTestManagerArgs _args;
        private WorkFlowResponse _workflowResponse;
        private WorkflowRunHelper _workflowRunHelper;

        public LogicAppTestManager(LogicAppTestManagerArgs args)
        {
            _args = args;
        }

        public void Setup()
        {
            //Setup authentication helper
            _authHelper = new AuthenticationHelper();
            _authHelper.ClientId = _args.ClientId;
            _authHelper.ClientSecret = _args.ClientSecret;
            _authHelper.TenantId = _args.TenantId;

            //Test we can authenticate and get a bearer token
            var bearerToken = _authHelper.GetBearerToken();

            //Setup Management API Helper
            _managementApiHelper = new ManagementApiHelper(_authHelper);

            //Setup Workflow Helper
            _workflowHelper = new WorkflowHelper();
            _workflowHelper.LogicAppName = _args.LogicAppName;
            _workflowHelper.ResourceGroupName = _args.ResourceGroupName;
            _workflowHelper.SubscriptionId = _args.SubscriptionId;
            _workflowHelper.WorkflowName = _args.WorkflowName;
            _workflowHelper.ManagementApiHelper = _managementApiHelper;
        }

        /// <summary>
        /// Trigger the logic app with an HTTP post
        /// </summary>
        /// <param name="content"></param>
        /// <param name="triggerName"></param>
        /// <returns></returns>
        public WorkFlowResponse TriggerLogicAppWithPost(StringContent content, string triggerName = "manual")
        {
            _workflowResponse = _workflowHelper.TriggerLogicAppWithPost(content, triggerName);
            return _workflowResponse;
        }

        /// <summary>
        /// Trigger the logic app with an HTTP get
        /// </summary>
        /// <param name="content"></param>
        /// <param name="triggerName"></param>
        /// <returns></returns>
        public WorkFlowResponse TriggerLogicAppWithGet(StringContent content, string triggerName = "manual")
        {
            _workflowResponse = _workflowHelper.TriggerLogicAppWithGet(content, triggerName);
            return _workflowResponse;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="triggerName"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public WorkFlowResponse RunLogicAppWorkflow(string workflowName, string triggerName, StringContent content)
        {
            _workflowResponse = _workflowHelper.RunLogicAppWorkflow(workflowName, triggerName, content);
            return _workflowResponse;
        }

        /// <summary>
        /// Gets the most recent completed workflow, if workflow is in running state than it will wait for it to complete
        /// </summary>
        /// <param name="workflowName"></param>
        /// <returns></returns>
        public RunDetails GetRecentCompletedRunDetails(string workflowName)
        {
            string runStatus = "";
            int interval = 1;
            RunDetails runDetails = null;
            do
            {
                runDetails = _workflowHelper.GetMostRecentRun(workflowName);

                runStatus = runDetails.properties.status;

                if (runStatus == "Running")
                {
                    //Sleep exponantial time window 
                    Thread.Sleep(2000 * interval);
                }
                interval++;

            //wait until workflow status is running, max wait time 5 mins
            } while (runStatus == "Running");

            return runDetails;
        }

        /// <summary>
        /// Loads the run history of the executed logic app.  This needs to be called before you start testing the actions
        /// that got executed
        /// </summary>
        public void LoadWorkflowRunHistory()
        {
            if (_workflowResponse == null)
                throw new Exception("You havent triggered the logic app");

            _workflowRunHelper = _workflowHelper.GetWorkflowRunHelper(_workflowResponse.WorkFlowRunId);
            _workflowRunHelper.GetRunActions(_workflowHelper.WorkflowName);
            _workflowRunHelper.GetRunDetails(_workflowHelper.WorkflowName);
        }

        public void LoadWorkflowRunHistory(string workflowName, string runId)
        {
            if (string.IsNullOrWhiteSpace(workflowName))
            {
                workflowName = _workflowHelper.WorkflowName;
            }
            _workflowRunHelper = _workflowHelper.GetWorkflowRunHelper(runId);
            _workflowRunHelper.GetRunActions(workflowName);
            _workflowRunHelper.GetRunDetails(workflowName);
        }

        public WorkflowRunList GetRunsSince(DateTime startDate)
        {
            return _workflowHelper.GetRunsSince(startDate);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startDate"></param>
        /// <returns></returns>
        public RunDetails GetMostRecentRunSince(DateTime startDate)
        {
            int interval = 1;
            RunDetails runDetails = null;
            do
            {
                runDetails = _workflowHelper.GetMostRecentRunDetails(startDate);

                if (runDetails == null)
                {
                    Thread.Sleep(2000 * interval);
                }
                interval++;

            } while (runDetails == null && interval < 1500);

            return runDetails;
        }

        public RunDetails GetMostRecentRun(string workflowName)
        {
            return _workflowHelper.GetMostRecentRun(workflowName);
        }

        public string GetMostRecentRunIdSince(DateTime startDate)
        {
            return _workflowHelper.GetMostRecentRunDetails(startDate).id;
        }

        /// <summary>
        /// Get the overall status of the workfow
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public WorkflowRunStatus GetWorkflowRunStatus(string workflowName, bool refresh = false)
        {
            var runDetails = _workflowRunHelper.GetRunDetails(workflowName, refresh);
            return runDetails.properties.WorkflowRunStatus;
        }

        /// <summary>
        /// Get the action from the run history so you can check if it was successful
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="refreshActions"></param>
        /// <param name="formatActionName"></param>
        /// <returns></returns>
        public ActionStatus GetActionStatus(string actionName, string workflowName, bool refreshActions = false, bool formatActionName = true)
        {
            return _workflowRunHelper.GetActionStatus(actionName, workflowName, refreshActions, formatActionName);
        }

        /// <summary>
        /// Get the input message to an action
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="refreshActions"></param>
        /// <param name="formatActionName"></param>
        /// <returns></returns>
        public string GetActionInputMessage(string actionName, string workflowName, bool refreshActions = false, bool formatActionName = true)
        {
            var action = _workflowRunHelper.GetActionJson(actionName, workflowName, refreshActions, formatActionName);
            var url = action["inputsLink"]?["uri"]?.Value<string>();
            var httpClient = new HttpClient();
            var response = httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }


        /// <summary>
        /// Get the input message to an action
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="refreshActions"></param>
        /// <param name="formatActionName"></param>
        /// <returns></returns>
        public string GetActionOutputMessage(string actionName, string workflowName, bool refreshActions = false, bool formatActionName = true)
        {
            var action = _workflowRunHelper.GetActionJson(actionName, workflowName, refreshActions, formatActionName);
            var url = action["outputsLink"]?["uri"]?.Value<string>();
            var httpClient = new HttpClient();
            var response = httpClient.GetAsync(url).Result;
            response.EnsureSuccessStatusCode();
            return response.Content.ReadAsStringAsync().Result;
        }

        /// <summary>
        /// Get the action json if you want to inspect it within your test
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="workflowName"></param>
        /// <param name="refreshActions"></param>
        /// <param name="formatActionName"></param>
        /// <returns></returns>
        public JToken GetActionJson(string actionName, string workflowName, bool refreshActions = false, bool formatActionName = true)
        {
            return _workflowRunHelper.GetActionJson(actionName, workflowName, refreshActions, formatActionName);
        }

        /// <summary>
        /// Get the trigger result so you can check its status
        /// </summary>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public TriggerStatus GetTriggerStatus(string workflowName, bool refresh = false)
        {
            return _workflowRunHelper.GetTriggerStatus(workflowName, refresh);
        }

        public string GetTriggerMessage(string workflowName, bool refresh = false)
        {
            return _workflowRunHelper.GetTriggerMessage(workflowName, refresh);
        }

        public GeneratedTestSample GenerateTestSample(string runId, bool includeSkippedSteps = false)
        {
            var sample = new GeneratedTestSample();

            _workflowRunHelper = _workflowHelper.GetWorkflowRunHelper(runId);
            var runActions = _workflowRunHelper.GetRunActions(_workflowHelper.WorkflowName);
            var runDetails = _workflowRunHelper.GetRunDetails(_workflowHelper.WorkflowName);

            var testCodeBuilder = new StringBuilder();
            testCodeBuilder.Append(Properties.Resources.SampleTestCode);
            testCodeBuilder.Replace("{{WorkflowName}}", _args.WorkflowName);

            var assertionsBuilder = new StringBuilder();
            foreach (JProperty actionProperty in (JToken)runActions.properties.actions)
            { 
                string name = actionProperty.Name;
                JToken value = actionProperty.Value;
                var actionDetails = JsonConvert.DeserializeObject<ActionDetails>(value.ToString());

                var includeAction = true;

                if (includeSkippedSteps == false && actionDetails.ActionStatus == ActionStatus.Skipped)
                    includeAction = false;

                if (includeAction)
                {
                    var tempText = Properties.Resources.SampleActionAssertion;
                    tempText = tempText.Replace("{{ActionName}}", name);
                    tempText = tempText.Replace("{{ActionStatus}}", actionDetails.status);
                    assertionsBuilder.Append(tempText);
                    assertionsBuilder.Append(Environment.NewLine);
                }
            }

            testCodeBuilder.Replace("{{ActionAssertions}}", assertionsBuilder.ToString());

            sample.TestSampleCode = testCodeBuilder.ToString();

            var triggerMessage = _workflowRunHelper.GetTriggerMessage(_workflowHelper.WorkflowName);
            try
            {
                //We will try to get the message body if its a json message such as HTTP with headers
                var jsonTriggerMessage = JObject.Parse(triggerMessage);
                if(jsonTriggerMessage.ContainsKey("body") &&
                    jsonTriggerMessage.ContainsKey("headers"))
                {
                    var body = jsonTriggerMessage["body"];
                    triggerMessage = body.ToString(Formatting.Indented);
                }
            }
            catch
            {

            }
            sample.TriggerMessage = triggerMessage;

            Console.WriteLine("Sample Code");
            Console.WriteLine("===========");

            Console.WriteLine(testCodeBuilder.ToString());

            Console.WriteLine("Sample Message for Test");
            Console.WriteLine("===========");
            Console.WriteLine(triggerMessage);
            Console.WriteLine("");
            return sample;
        }
    }
}
