
namespace TfsCheckInProgrammatically 
{
  
    using Microsoft.TeamFoundation.Client;
    using Microsoft.TeamFoundation.VersionControl.Client;
    using System.Collections.Generic;
    using Microsoft.TeamFoundation.WorkItemTracking.Client;
    using System.Windows.Forms;
    [SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions", Justification = "Sample code")]
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1300:ElementMustBeginWithUpperCaseLetter", Justification = "Default event handler naming pattern")]

    public partial class TfsCheckIn : System.Windows.Controls.UserControl
    {

        #region Private tfsProperties
        private TfsTeamProjectCollection teamProjectCollection = null;
        private WorkItemStore WIStore = null;
        private Uri collectionUri = null;
        private Workspace workspace = null;
        private WorkItem workItem = null;
        private VersionControlServer versionControl = null;
        #endregion Private tfsProperties

        
        #region Constructors
        public TfsCheckIn()
        {
            collectionUri = new Uri("http://devtfsserver:8080/tfs/defaultcollection");
            InitializeTfsConfiguration();
            this.InitializeComponent();
            if (txtScript.Text == "")
            {
                btnCommit.IsEnabled = false;
            }
        }
        #endregion Constructors

       
        #region Initialize Connection to TFS
        private void InitializeTfsConfiguration()
        {

            teamProjectCollection = new TfsTeamProjectCollection(collectionUri);

            WIStore = (WorkItemStore)teamProjectCollection.GetService(typeof(WorkItemStore));

            versionControl = (VersionControlServer)teamProjectCollection.GetService(typeof(VersionControlServer));
            //VersionControlServer versionControl = teamProjectCollection.GetService<VersionControlServer>();

        }
        #endregion Initialize Connection to TFS

        #region Pending Method
        public void DoPendingChanges()
        {
            var numberOfChange = workspace.PendEdit(filePathToCheckIn);

            Conflict[] conflicts = workspace.QueryConflicts(new string[] { "$/TestProject/ScriptsTest" + Path.DirectorySeparatorChar + txtPbiId.Text + ".sql" }, true);
            if (conflicts != null && conflicts.Length > 0)
            {
                //System.Windows.MessageBox.Show("conflict");
                foreach (Conflict conflict in conflicts)
                {
                    if (workspace.MergeContent(conflict, false))
                    {
                        //System.Windows.MessageBox.Show("conflict1");
                        conflict.Resolution = Resolution.AcceptYours;
                        workspace.ResolveConflict(conflict);
                    }
                }
            }
            else
            {
                numberOfChange = workspace.PendAdd(filePathToCheckIn, true);
            }
        }
        #endregion Pending Method

        #region Setting Workspace Properties
        public string GetWorkSpacesOwnerName()
        {
            String ownerName = null;

            InitializeTfsConfiguration();

            var workspaces = versionControl.QueryWorkspaces(null, versionControl.AuthenticatedUser, System.Net.Dns.GetHostName().ToString());

            foreach (var ws in workspaces)
            {
                ownerName = ws.OwnerName;
            }
            //System.Windows.MessageBox.Show(ownerName);
            return ownerName;
        }

        public void FillNameOFWorkSpaces()
        {
            InitializeTfsConfiguration();

            var workspaces = versionControl.QueryWorkspaces(null, versionControl.AuthenticatedUser, System.Net.Dns.GetHostName().ToString());

            foreach (var ws in workspaces)
            {
                //Filling name of workspaces into combobox.
               // comboBoxWorkspaces.Items.Add(ws.Name.ToString());
            }
        }
        #endregion Setting Workspace Properties

        #region TFS Check In 
        public void TfsCheckIn()
        {
            var sourceLocation = "$/TestProject/ScriptsTest";
            try
            {
                //There is a method above to connect TFS.   
                InitializeTfsConfiguration();

                var user = versionControl.AuthorizedUser;

                workspace = versionControl.GetWorkspace(comboBoxWorkspaces.Text, GetWorkSpacesOwnerName());
                directory = workspace.GetLocalItemForServerItem(sourceLocation);

                System.Windows.MessageBox.Show(directory);

                filePathToCheckIn = directory + "\\" + txtPbiId.Text + ".sql";
                System.Windows.MessageBox.Show(filePathToCheckIn);

                teamProjectCollection.Authenticate();

                /* workspace.Map("$/TestProject/ScriptsTest", directory);

                 workspace.Get();*/

                FileIOHelper.fileWriter(filePathToCheckIn, txtScript);

                workItem = WIStore.GetWorkItem(Convert.ToInt32(txtPbiId.Text));
                var WIChecInInfo = new[]
                {
                    new WorkItemCheckinInfo(workItem, WorkItemCheckinAction.Associate)
                };

                var items = versionControl.GetItems(directory, VersionSpec.Latest, RecursionType.Full);

                //The file which want to be checked will pretend to be new file which has same path string array below.
                String[] newFileForTfs = new string[1] { sourceLocation + Path.DirectorySeparatorChar + txtPbiId.Text + ".sql" };
                //Then getting  latest version of this file will show all changes on TFS part.
                workspace.Get(newFileForTfs, VersionSpec.Latest, RecursionType.Full, GetOptions.Overwrite);
                //After that, pending changes can be implemented.
                DoPendingChanges();

                Workstation.Current.EnsureUpdateWorkspaceInfoCache(versionControl, GetWorkSpacesOwnerName());
                PendingChange[] pendingChanges = workspace.GetPendingChanges(filePathToCheckIn, RecursionType.Full);

                workspace.CheckIn(pendingChanges, txtDescription.Text, null, WIChecInInfo, null);

                MessageBox.InformationMessage("    Checked In Successfully    ");
            }
            catch (Exception ex)
            {
                MessageBox.ErrorMessage(ex.Message);
            }
        }//End of TFS check in code.
        #endregion TFS Check In 

}