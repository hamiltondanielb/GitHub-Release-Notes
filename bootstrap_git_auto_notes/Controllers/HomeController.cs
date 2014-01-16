using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using bootstrap_git_auto_notes.GitClasses;
using RestSharp;
using System.Xml.Linq;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System.IO;


namespace bootstrap_git_auto_notes.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private string clientURL = "https://api.github.com";
        private string username = "sjwsgm@gmail.com";
        private string password = "92SWeaves2";
        private string owner = "stephenweaver";
        private string org = "ININServices";

        // Set this to whatever label is going to indicate that a github issue is NOT aa bug/defect
        private string feature_label_name = "enhancement";

        
        public ActionResult Index()
        {
            try
            {
                List<Repository> repositories = GetAllRepositories(clientURL, username, password);
                
                ViewBag.repositories = repositories;

                return View();
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }

        [HttpPost]
        public ActionResult RepoReleases(string repository)
        {
            try
            {
                List<Release> releases = GetReleases(clientURL, username, password, owner, repository);

                ViewBag.releases = releases;
                ViewBag.repo = repository.Replace(owner + "/", String.Empty);

                return View();
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }


        [HttpPost]
        public ActionResult ReleaseNotes(string txb_ToRelease, string txb_FromRelease, string repo)
        {
            try
            {
                Label feature_label = new Label();
                List<Release> releases = GetReleases(clientURL, username, password, owner, repo);

                BaseCommit startCommit = GetSingleCommit(clientURL, username, password, owner, repo, txb_FromRelease);
                BaseCommit endCommit = GetSingleCommit(clientURL, username, password, owner, repo, txb_ToRelease);

                List<Milestone> milestones = GetAllMilestones(clientURL, username, password, owner, repo);
                List<Issue> issues = GetAllIssues(clientURL, username, password, owner, repo);
                List<Label> labels = GetLabels(clientURL, username, password, owner, repo);

                foreach (Label label in labels)
                {
                    if (label.name == feature_label_name)
                    {
                        feature_label = label;
                        break;
                    }
                }

                Compare comparison = CompareCommits(clientURL, username, password, owner, repo, startCommit.sha, endCommit.sha);
                List<BaseCommit> commits = GetCommits(clientURL, username, password, owner, repo, null, null, null);

                List<Issue> issues_openANDbelong = new List<Issue>();
                List<Issue> issues_openNOTbelong = new List<Issue>();
                List<Issue> issues_closedNOTbelong = new List<Issue>();
                List<Issue> features_closed = new List<Issue>();
                // This commit is acting funny
                foreach (Issue issue in issues)
                {
                    if (issue.created_at < endCommit.commit.author.date)
                    {
                        if (issue.state.CompareTo("open") == 0)
                        {
                            if (issue.created_at < startCommit.commit.author.date)
                                if(issue.labels.Contains(feature_label))
                                issues_openNOTbelong.Insert(0, issue);
                            else
                                issues_openANDbelong.Insert(0, issue);
                        }
                        else if (issue.state.CompareTo("closed") == 0)
                        {
                            if (issue.labels.Contains(feature_label) && issue.closed_at < endCommit.commit.author.date)
                            {
                                features_closed.Insert(0, issue);
                            }
                            else
                            {
                                if (issue.created_at < startCommit.commit.author.date &&
                                   issue.closed_at > startCommit.commit.author.date &&
                                   issue.closed_at < endCommit.author.date)
                                {
                                    issues_closedNOTbelong.Insert(0, issue);
                                }
                                else
                                {
                                    // These are issues that were closed after the last commit of this comparison and are therefore irrelevant
                                }
                            }
                        }
                        else
                        {
                            throw new Exception("An issue should always be either open or or closed");
                        }
                    }
                    else
                    {
                        // These are issues created after the endCommit date
                    }
                }

                ViewBag.issues_openNOTbelong = issues_openNOTbelong;
                ViewBag.issues_openANDbelong = issues_openANDbelong;
                ViewBag.issues_closedNOTbelong = issues_closedNOTbelong;
                ViewBag.features_closed = features_closed;

                ViewBag.repo = repo;
                ViewBag.FromRelease = txb_FromRelease;
                ViewBag.ToRelease = txb_ToRelease;
                commits.Reverse();
                ViewBag.commits = commits;

                ViewBag.releases = releases;
                return View();
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }

        #region Label Manager
        public ActionResult ManageLabels()
        {
            List<Repository> repositories = GetAllRepositories(clientURL, username, password);
            List<Label> all_labels = new List<Label>();
            foreach (Repository repo in repositories)
            {
                string sRepo = repo.name.Replace(owner + "/", String.Empty);
                all_labels.AddRange(GetLabels(clientURL, username, password, owner, sRepo));
            }
            ViewBag.labels = all_labels.Where(label => label.name != null).Select(label => label.name).Distinct();

            return View();
        }

        [HttpPost]
        public ActionResult AddLabelToAllRepos(string label_names_text)
        {
            List<string> label_names = label_names_text.Split(',').ToList();
            label_names = label_names.Select(name => name.Trim()).ToList();

            List<string> labels_added = new List<string>();
            List<string> labels_not_added = new List<string>();

            var random = new Random();
            var color = String.Format("{0:x6}", random.Next(0x1000000));
            RestClient client;

            List<Repository> repositories = GetAllRepositories(clientURL, username, password);

            foreach (Repository repo in repositories)
            {
                string repo_name = repo.name.Replace(owner + "/", String.Empty);
                
                foreach (string label_name in label_names)
                {
                    client = new RestClient(clientURL);
                    client.Authenticator = new HttpBasicAuthenticator(username, password);
                    RestRequest request = new RestRequest("repos/{owner}/{repo}/labels", Method.POST);
                    request.AddUrlSegment("owner", owner);
                    request.AddUrlSegment("repo", repo_name);
                    request.RequestFormat = DataFormat.Json;
                    request.AddBody(new { name = label_name, color = color });
                    var temp = client.Execute(request);
                    if (temp.ResponseStatus == ResponseStatus.Completed && temp.StatusCode == System.Net.HttpStatusCode.Created)
                    {
                        labels_added.Add(label_name);
                        labels_not_added.Remove(label_name);
                    }
                    else
                    {
                        if (!labels_added.Contains(label_name))
                        {
                            labels_not_added.Add(label_name);
                        }
                    }
                }
            }
            
            // Create return message
            string info_message = string.Empty;
            int return_type = 0;

            if (labels_added.Count > 0)
            {
                return_type += 1;               
                info_message += "\"" + String.Join(", ", labels_added.Distinct().ToArray()) + "\" has been added to the repositories as a label";
            }
            
            if(labels_not_added.Count > 0)
            {
                return_type += 2; 
                if(info_message != string.Empty) {info_message += "<br \\>";}
                info_message += "\"" + String.Join(", ", labels_not_added.Distinct().ToArray()) + "\" was NOT added to any repository. (It probably already existed)";
            }

            switch (return_type)
            {
                case 1: return RedirectToAction("ManageLabels").Success(info_message);
                case 2: return RedirectToAction("ManageLabels").Warning(info_message);
                case 3: return RedirectToAction("ManageLabels").Information(info_message);
            }
            
            return RedirectToAction("ManageLabels").Error("No labels were added");
        }

        [HttpPost]
        public ActionResult RemoveLabelFromAllRepos(string label_name)
        {
            RestClient client;
            bool removed = false;
            List<Repository> repositories = GetAllRepositories(clientURL, username, password);
            foreach (Repository repo in repositories)
            {
                string repo_name = repo.name.Replace(owner + "/", String.Empty);
                List<Label> labels = GetLabels(clientURL, username, password, owner, repo_name);
                foreach (Label label in labels)
                {
                    if (String.Compare(label.name, label_name) == 0)
                    {
                        client = new RestClient(clientURL);
                        client.Authenticator = new HttpBasicAuthenticator(username, password);
                        RestRequest request = new RestRequest("repos/{owner}/{repo}/labels/{name}", Method.DELETE);
                        request.RequestFormat = DataFormat.Json;
                        request.AddUrlSegment("owner", owner);
                        request.AddUrlSegment("repo", repo_name);
                        request.AddUrlSegment("name", label_name);

                        var temp = client.Execute(request);
                        if (temp.ResponseStatus == ResponseStatus.Completed && temp.StatusCode == System.Net.HttpStatusCode.NoContent)
                        {
                            removed = true;
                        }
                    }
                }
            }
            if (removed)
            {
                return RedirectToAction("ManageLabels").Success("\"" + label_name + "\" has been removed from all the repositories");
            }
            else
            {
                return RedirectToAction("ManageLabels").Warning("\"" + label_name + "\" was NOT removed from any repository. Does this label even exist?");
            }
        }

        [HttpPost]
        public ActionResult RenameLabelForAllRepos(string old_label_name, string new_label_name)
        {
            RestClient client;
            bool changed = false;
            List<Repository> repositories = GetAllRepositories(clientURL, username, password);
            foreach (Repository repo in repositories)
            {
                string repo_name = repo.name.Replace(owner + "/", String.Empty);
                List<Label> labels = GetLabels(clientURL, username, password, owner, repo_name);
                foreach (Label label in labels)
                {
                    if (String.Compare(label.name, old_label_name) == 0)
                    {
                        client = new RestClient(clientURL);
                        client.Authenticator = new HttpBasicAuthenticator(username, password);
                        RestRequest request = new RestRequest("repos/{owner}/{repo}/labels/{name}", Method.PATCH);
                        request.RequestFormat = DataFormat.Json;
                        request.AddUrlSegment("owner", owner);
                        request.AddUrlSegment("repo", repo_name);
                        request.AddUrlSegment("name", old_label_name);

                        request.AddBody(new { name = new_label_name, color = label.color });

                        var temp = client.Execute(request);
                        if (temp.ResponseStatus == ResponseStatus.Completed && temp.StatusCode == System.Net.HttpStatusCode.OK)
                        {
                            changed = true;
                        }
                    }
                }
            }
            if (changed)
            {
                return RedirectToAction("ManageLabels").Success("\"" + old_label_name + "\" has been changed to \"" + new_label_name + "\" in all the repositories");
            }
            else
            {
                return RedirectToAction("ManageLabels").Warning("\"" + old_label_name + "\" was NOT changed to \"" + new_label_name + "\"  in any of the repositories");
            }
        }

        #endregion

        [HttpPost]
        public ActionResult DownloadDocx(FormCollection collection)
        {
            try
            {
                int counter = 1;
                MemoryStream ms;
                string cell_color = "";

                // Create an empty table.
                Table table = new Table();

                // Create a TableProperties object and specify its border information.
                TableProperties tblProp = new TableProperties(
                    new TableBorders(
                        new InsideHorizontalBorder()
                        {
                            Val = new EnumValue<BorderValues>(BorderValues.Single),
                            Size = 1,
                            Color = "dddddd"
                        }
                    )
                );

                // Append the TableProperties object to the empty table.
                table.AppendChild<TableProperties>(tblProp);

                // Create a row.
                while (!String.IsNullOrEmpty(collection["record_sha_" + counter++]))
                {
                    if (String.Compare(collection["checkbox_" + (counter - 1).ToString()], "on") == 0)
                    {
                        TableRow tr = new TableRow();

                        if (counter % 2 == 1)
                            cell_color = "ffffff";
                        else if (counter % 2 == 0)
                            cell_color = "f9f9f9";
                        else
                            throw new Exception();

                        tr.Append(CreateTableCell(collection["record_message_" + (counter - 1).ToString()], cell_color, 8500));
                        tr.Append(CreateTableCell(collection["record_sha_" + (counter - 1).ToString()].Substring(0, 7), cell_color, 1000));

                        // Append the table row to the table.
                        table.Append(tr);

                    }
                }

                // Set up what is being recieved via the form
                Dictionary<string, string> issueTypeList = new Dictionary<string, string>();
                issueTypeList.Add("features_closed", "Features added in this release:");
                issueTypeList.Add("closedNOTbelong", "Issues fixed in this release:");
                issueTypeList.Add("openANDbelong", "Known issues created/found in this release:");
                issueTypeList.Add("openNOTbelong", "Pre-existing issues not fixed in this release:");

                using (ms = new MemoryStream())
                {
                    using (WordprocessingDocument wordDocument = WordprocessingDocument.Create(ms, WordprocessingDocumentType.Document))
                    {
                        MainDocumentPart mainPart = wordDocument.AddMainDocumentPart();

                        mainPart.Document = new Document(new Body());

                        AddTitle("Release notes from " + collection["fromRelease"] + " to " + collection["toRelease"], mainPart.Document.Body, 40);

                        // Create the list issue data
                        foreach (KeyValuePair<string, string> listname in issueTypeList)
                        {
                            AddBlankLine(mainPart.Document.Body);
                            AddTitle(listname.Value, mainPart.Document.Body, 30);
                            AddList_FromFormData_ByTitle(collection, listname.Key, mainPart.Document.Body);
                        }

                        // Append the commit log table to the document.
                        AddBlankLine(mainPart.Document.Body);
                        AddTitle("The Commit Log:", mainPart.Document.Body, 30);

                        mainPart.Document.Body.Append(table);
                    }
                }

                return File(ms.ToArray(), "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "Test.docx");
            }
            catch (Exception ex)
            {
                return View("Error");
            }
        }

        #region wordDocumentCreationMethods
        private void AddBlankLine(Body document)
        {
            document.Append(new Paragraph(new Run(new Text(""))));
        }

        private void AddTitle(String title, Body document, int textSize)
        {
            document.Append(new Paragraph(new Run(new Text(title))
            {
                RunProperties = new RunProperties() { FontSize = new FontSize() { Val = textSize.ToString() }, Bold = new Bold() }
            })
            {
                ParagraphProperties = new ParagraphProperties()
                {
                    ContextualSpacing = new ContextualSpacing()
                }
            });
        }

        private void AddList_FromFormData_ByTitle(FormCollection formData, string dataName, Body document)
        {
            int counter = 0;
            string openNOTbelong = string.Empty;
            while (!String.IsNullOrEmpty(formData[dataName + "-" + ++counter]))
            {
                Paragraph p = new Paragraph(new Run(new Text(formData[dataName + "-" + counter])))
                {
                    ParagraphProperties = new ParagraphProperties()
                    {
                        ContextualSpacing = new ContextualSpacing(),
                        ParagraphStyleId = new ParagraphStyleId() { Val = "ListParagraph" },
                        NumberingProperties = new NumberingProperties()
                        {
                            NumberingId = new NumberingId() { Val = 1 },
                            NumberingLevelReference = new NumberingLevelReference() { Val = 0 }
                        }
                    }
                };
                document.Append(p);
            }
        }

        private TableCell CreateTableCell(string contents, string cell_color = "ffffff", int width = 0)
        {
            // Create the TableCell object
            TableCell tc = new TableCell();

            TableCellProperties tcp;
            if (width == 0)
            {
                // Create the TableCellProperties object
                tcp = new TableCellProperties(
                    new TableCellWidth { Type = TableWidthUnitValues.Auto }
                );
            }
            else
            {
                tcp = new TableCellProperties(
                    new TableCellWidth() { Type = TableWidthUnitValues.Dxa, Width = width.ToString() }
                );
            }


            // Create the Shading object
            DocumentFormat.OpenXml.Wordprocessing.Shading shading =
                new DocumentFormat.OpenXml.Wordprocessing.Shading()
                {
                    Color = "auto",
                    Fill = cell_color,
                    Val = ShadingPatternValues.Clear
                };

            // Add the Shading object to the TableCellProperties object
            tcp.Append(shading);

            // Add the TableCellProperties object to the TableCell object
            tc.PrependChild<TableCellProperties>(tcp);

            // also need to ensure you include the text, otherwise it causes an error (it did for me!)
            Text text = new Text(contents);

            // Specify the table cell content.
            Run run = new Run();
            RunProperties runProp = new RunProperties(); // Create run properties.
            RunFonts runFont = new RunFonts();           // Create font
            runFont.Ascii = "Arial";                     // Specify font family

            FontSize size = new FontSize();
            size.Val = new StringValue("22");  // 48 half-point font size
            runProp.Append(runFont);
            runProp.Append(size);

            run.PrependChild<RunProperties>(runProp);
            run.Append(text);

            

            ParagraphProperties pProperties = new ParagraphProperties();
            ContextualSpacing cs = new ContextualSpacing();
            pProperties.Append(cs);
            Paragraph paragraph = new Paragraph(run);
            paragraph.PrependChild<ParagraphProperties>(pProperties);
            tc.Append(paragraph);

            return tc;
        }
        #endregion

        #region gitHubAPIcallHelps
        


        private List<Repository> GetAllRepositories(string clientURL, string username, string password)
        {
            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            //RestRequest request = new RestRequest("/orgs/ININServices/repos", Method.GET);
            RestRequest request = new RestRequest("/user/repos", Method.GET);

            return client.Execute<List<Repository>>(request).Data;
        }

        private List<Milestone> GetAllMilestones(string clientURL, string username, string password, string owner, string repo)
        {
            List<Milestone> allMiletones = new List<Milestone>();

            allMiletones.AddRange(GetMilestones(clientURL, username, password, owner, repo, "closed"));
            allMiletones.AddRange(GetMilestones(clientURL, username, password, owner, repo, "open"));

            return allMiletones;
        }

        private List<Issue> GetAllIssues(string clientURL, string username, string password, string owner, string repo)
        {
            List<Issue> allIssues = new List<Issue>();

            allIssues.AddRange(GetIssues(clientURL, username, password, owner, repo, "closed", "*"));
            allIssues.AddRange(GetIssues(clientURL, username, password, owner, repo, "open", "*"));
            allIssues.AddRange(GetIssues(clientURL, username, password, owner, repo, "closed", "none"));
            allIssues.AddRange(GetIssues(clientURL, username, password, owner, repo, "open", "none"));

            return allIssues;
        }


        private List<Milestone> GetMilestones(string clientURL, string username, string password, string owner, string repo, string state)
        {
            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            RestRequest request = new RestRequest("repos/{owner}/{repo}/milestones", Method.GET);
            request.AddParameter("state", state);
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);
            return client.Execute<List<Milestone>>(request).Data;
        }

        private List<Issue> GetIssues(string clientURL, string username, string password, string owner, string repo, string state, string milestone)
        {
            List<Issue> issues = new List<Issue>();
            List<Issue> newIssues = new List<Issue>();

            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);

            // Get no assignee
            RestRequest request = new RestRequest("repos/{owner}/{repo}/issues", Method.GET);
            request.AddParameter("milestone", milestone);
            request.AddParameter("state", state);
            request.AddParameter("assignee", "none");
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);
            newIssues = client.Execute<List<Issue>>(request).Data;

            if (newIssues != null && newIssues.Count > 0)
                issues.AddRange(newIssues);

            // Get with assingee
            request = new RestRequest("repos/{owner}/{repo}/issues", Method.GET);
            request.AddParameter("milestone", milestone);
            request.AddParameter("state", state);
            request.AddParameter("assignee", "*");
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);
            newIssues = client.Execute<List<Issue>>(request).Data;

            if (newIssues != null && newIssues.Count > 0)
                issues.AddRange(newIssues);

            return issues;
        }

        private List<Label> GetLabels(string clientURL, string username, string password, string owner, string repo)
        {
            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            RestRequest request = new RestRequest("repos/{owner}/{repo}/labels", Method.GET);
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);
            return client.Execute<List<Label>>(request).Data;
        }

        private List<Release> GetReleases(string clientURL, string username, string password, string owner, string repo)
        {
            repo = repo.Replace(owner + "/", String.Empty);
            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            RestRequest request = new RestRequest("repos/{owner}/{repo}/releases", Method.GET);
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);

            List<Release> releases = client.Execute<List<Release>>(request).Data;
            releases.Reverse();
            return releases;
        }

        private Compare CompareCommits(string clientURL, string username, string password, string owner, string repo, string commit1, string commit2)
        {
            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            RestRequest request = new RestRequest("repos/{owner}/{repo}/compare/{base}...{head}", Method.GET);
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);
            request.AddUrlSegment("base", commit1);
            request.AddUrlSegment("head", commit2);
            return client.Execute<Compare>(request).Data;
        }


        private List<BaseCommit> GetCommits(string clientURL, string username, string password, string owner, string repo, string sha, string since, string until)
        {
            List<BaseCommit> someCommits = new List<BaseCommit>();

            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            RestRequest request = new RestRequest("repos/{owner}/{repo}/commits", Method.GET);
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);

            if (sha != null)
                request.AddParameter("sha", sha);
            if (since != null)
                request.AddParameter("since", since);
            if (until != null)
                request.AddParameter("until", until);

            someCommits.AddRange(client.Execute<List<BaseCommit>>(request).Data);

            if (someCommits.Count == 30)
            {
                string lastSHA = someCommits[29].sha;
                someCommits.RemoveAt(29);
                someCommits.AddRange(GetCommits(clientURL, username, password, owner, repo, lastSHA, since, until));
            }

            return someCommits;
        }


        private BaseCommit GetSingleCommit(string clientURL, string username, string password, string owner, string repo, string sha)
        {
            RestClient client = new RestClient(clientURL);
            client.Authenticator = new HttpBasicAuthenticator(username, password);
            RestRequest request = new RestRequest("repos/{owner}/{repo}/commits/{sha}", Method.GET);
            request.AddUrlSegment("owner", owner);
            request.AddUrlSegment("repo", repo);
            request.AddUrlSegment("sha", sha);

            return client.Execute<BaseCommit>(request).Data;
        }

        #endregion

        public ActionResult About()
        {
            ViewBag.Message = "Your app description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }

    public class CompareModel
    {
        public string Start { get; set; }
        public string Stop { get; set; }
    }
}