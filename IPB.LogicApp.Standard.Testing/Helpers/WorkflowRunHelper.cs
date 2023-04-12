using IPB.LogicApp.Standard.Testing.Model;
using IPB.LogicApp.Standard.Testing.Model.WorkflowRunActionDetails;
using IPB.LogicApp.Standard.Testing.Model.WorkflowRunOverview;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Net.Http;

namespace IPB.LogicApp.Standard.Testing.Helpers
{
    public class WorkflowRunHelper
    {
        private WorkflowRunActionDetails _runActions;
        private RunDetails _runDetails;

        public WorkflowHelper WorkflowHelper { get; set; }


        public ManagementApiHelper ManagementApiHelper { get; set; }

        public string RunId { get; set; }

        public bool WasRunSuccessful()
        {
            var runDetails = GetRunDetails(WorkflowHelper.WorkflowName);
            if (runDetails.properties.status == "Succeeded")
                return true;
            else
                return false;
        }

        /// <summary>
        /// This will get the run actions for the workflow if we dont already have them and get the specific action for the name we want and
        /// return the action status.
        /// Note that if the action name has spaces in it the default behaviour is to replace spaces with underscore which is what azure does
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="actionName"></param>
        /// <param name="refreshActions"></param>
        /// <param name="formatActionName"></param>
        /// <returns></returns>
        public ActionStatus GetActionStatus(string workflowName, string actionName, bool refreshActions = false, bool formatActionName = true)
        {
            //The run history usually formats the name of the action to not have spaces and it replaces then with underscores
            if (formatActionName)
                actionName = actionName.Replace(" ", "_");

            if (refreshActions || _runActions == null)
                _runActions = GetRunActions(workflowName, refreshActions);

            var action = _runActions.properties.GetAction(actionName);
            if (action == null)
                return ActionStatus.ActionDoesntExistInRunHistory;

            return action.ActionStatus;
        }

        /// <summary>
        /// Gets Json value of given action (step) for specified logicapp workflow
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="actionName"></param>
        /// <param name="refreshActions"></param>
        /// <param name="formatActionName"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public JToken GetActionJson(string workflowName, string actionName, bool refreshActions = false, bool formatActionName = true)
        {
            //The run history usually formats the name of the action to not have spaces and it replaces then with underscores
            if (formatActionName)
                actionName = actionName.Replace(" ", "_");

            if (refreshActions || _runActions == null)
                _runActions = GetRunActions(workflowName);

            var action = _runActions.properties.GetActionJson(actionName);
            if (action == null)
                throw new Exception("The action does not exist");

            return action;
        }

        /// <summary>
        /// Gets the trigger status of a logic app workflow by workflow name.
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public TriggerStatus GetTriggerStatus(string workflowName, bool refresh = false)
        {
            if (refresh || _runActions == null)
                _runActions = GetRunActions(workflowName, refresh);

            return _runActions.properties.trigger.TriggerStatus;
        }


        /// <summary>
        /// Gets the trigger details for a logic app workflow by workflow name, this works for child workflow as well
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public TriggerDetails GetTriggerDetails(string workflowName, bool refresh = false)
        {
            if (refresh || _runActions == null)
                _runActions = GetRunActions(workflowName, refresh);

            return _runActions.properties.trigger;
        }

        /// <summary>
        /// Get logic app trigger message as string for given logic app workflow by workflow name
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public string GetTriggerMessage(string workflowName, bool refresh = false)
        {
            if (refresh || _runActions == null)
                _runActions = GetRunActions(workflowName, refresh);

            var url = _runActions.properties.trigger.outputsLink.uri;
            var httpClient = new HttpClient();

            HttpResponseMessage response = httpClient.GetAsync(url).Result;
            var responseText = response.Content.ReadAsStringAsync().Result;
            response.EnsureSuccessStatusCode();
            return responseText;
        }



        /// <summary>
        /// Gets the run details from Azure if we dont already have them
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public RunDetails GetRunDetails(string workflowName, bool refresh = false)
        {
            //if workflow name is empty than use parent (main) workflow
            if (string.IsNullOrWhiteSpace(workflowName))
            {
                workflowName = WorkflowHelper.WorkflowName;
            }
            var url = $@"subscriptions/{WorkflowHelper.SubscriptionId}/resourceGroups/{WorkflowHelper.ResourceGroupName}/providers/Microsoft.Web/sites/{WorkflowHelper.LogicAppName}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}/runs/{RunId}?api-version={ApiSettings.ApiVersion}";

            if (refresh || _runDetails == null)
            {
                var client = ManagementApiHelper.GetHttpClient();

                HttpResponseMessage response = client.GetAsync(url).Result;
                var responseText = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();

                _runDetails = JsonConvert.DeserializeObject<RunDetails>(responseText);
            }
            return _runDetails;
        }

        /// <summary>
        /// Gets run action details based on logic app workflow name
        /// </summary>
        /// <param name="workflowName"></param>
        /// <param name="refresh"></param>
        /// <returns></returns>
        public WorkflowRunActionDetails GetRunActions(string workflowName, bool refresh = false)
        {
            //if workflow name is empty than use parent (main) workflow
            if (string.IsNullOrWhiteSpace(workflowName))
            {
                workflowName = WorkflowHelper.WorkflowName;
            }
            var url = $@"subscriptions/{WorkflowHelper.SubscriptionId}/resourceGroups/{WorkflowHelper.ResourceGroupName}/providers/Microsoft.Web/sites/{WorkflowHelper.LogicAppName}/hostruntime/runtime/webhooks/workflow/api/management/workflows/{workflowName}/runs/{RunId}?api-version={ApiSettings.ApiVersion}&$expand=properties/actions,workflow/properties";
            if (refresh || _runActions == null)
            {
                var client = ManagementApiHelper.GetHttpClient();

                HttpResponseMessage response = client.GetAsync(url).Result;
                var responseText = response.Content.ReadAsStringAsync().Result;
                response.EnsureSuccessStatusCode();

                _runActions = JsonConvert.DeserializeObject<WorkflowRunActionDetails>(responseText);
            }
            return _runActions;
        }
    }
}
