using System.Threading;
using System.Text.RegularExpressions;

//////////////////////////////////////////////////////////////////////
// ADDINS
//////////////////////////////////////////////////////////////////////

#addin "Cake.FileHelpers"
#addin "Cake.AppleSimulator"
#addin "Cake.Android.Adb"
#addin "Cake.Xamarin"
//////////////////////////////////////////////////////////////////////
// TOOLS
//////////////////////////////////////////////////////////////////////

#tool "GitReleaseManager"
#tool "GitVersion.CommandLine"
#tool "GitLink"
#tool "nuget:?package=xunit.runner.console"
using Cake.Common.Build.TeamCity;
using Cake.AppleSimulator.UnitTest;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


//////////////////////////////////////////////////////////////////////
// ARGUMENTS
//////////////////////////////////////////////////////////////////////

var target = Argument("target", "Default");

if (string.IsNullOrWhiteSpace(target))
{
    target = "Default";
}


//////////////////////////////////////////////////////////////////////
// PREPARATION
//////////////////////////////////////////////////////////////////////

// Should MSBuild & GitLink treat any errors as warnings?
var treatWarningsAsErrors = false;
Func<string, int> GetEnvironmentInteger = name => {
	
	var data = EnvironmentVariable(name);
	int d = 0;
	if(!String.IsNullOrEmpty(data) && int.TryParse(data, out d)) 
	{
		return d;
	} 
	
	return 0;

};

// Load json Configuartion
var configFilePath = "./config.json";
JObject config;

if(!FileExists(configFilePath)) {
	
	throw new Exception(string.Format("config.json can not be found at {0}", configFilePath));
}

var configFile = File(configFilePath);

using(var stream = new StreamReader(System.IO.File.OpenRead(configFile.Path.FullPath))) {
	var json = stream.ReadToEnd();
	config = JObject.Parse(json);
};

if(config == null) {
	throw new Exception(string.Format("config.json can not be found at {0}", configFilePath));
}

// Build configuration
var productName = config.Value<string>("productName");
var project = config.Value<string>("projectName");
var local = BuildSystem.IsLocalBuild;
var isTeamCity = BuildSystem.TeamCity.IsRunningOnTeamCity;
var isRunningOnUnix = IsRunningOnUnix();
var isRunningOnWindows = IsRunningOnWindows();
var teamCity = BuildSystem.TeamCity;
var branch = EnvironmentVariable("Git_Branch");
var isPullRequest = !String.IsNullOrEmpty(branch) && branch.ToUpper().Contains("PULL-REQUEST"); //teamCity.Environment.PullRequest.IsPullRequest;
var projectName =  EnvironmentVariable("TEAMCITY_PROJECT_NAME"); //  teamCity.Environment.Project.Name;
var isRepository = StringComparer.OrdinalIgnoreCase.Equals(productName, projectName);
var isTagged = !String.IsNullOrEmpty(branch) && branch.ToUpper().Contains("TAGS");
var buildConfName = EnvironmentVariable("TEAMCITY_BUILDCONF_NAME"); //teamCity.Environment.Build.BuildConfName
var buildNumber = GetEnvironmentInteger("BUILD_NUMBER");
var isReleaseBranch = StringComparer.OrdinalIgnoreCase.Equals("master", buildConfName) || StringComparer.OrdinalIgnoreCase.Equals("Release", buildConfName);
var shouldAddLicenseHeader = false;
if(!string.IsNullOrEmpty(EnvironmentVariable("ShouldAddLicenseHeader"))) {
	shouldAddLicenseHeader = bool.Parse(EnvironmentVariable("ShouldAddLicenseHeader"));
}

var githubOwner = config.Value<string>("githubOwner");
var githubRepository = config.Value<string>("githubRepository");
var githubUrl = string.Format("https://github.com/{0}/{1}", githubOwner, githubRepository);
var licenceUrl = string.Format("{0}/blob/master/LICENSE", githubUrl);

// Version
var gitVersion = GitVersion();
var majorMinorPatch = gitVersion.MajorMinorPatch;
var semVersion = gitVersion.SemVer;
var informationalVersion = gitVersion.InformationalVersion;
var nugetVersion = gitVersion.NuGetVersion;
var buildVersion = gitVersion.FullBuildMetaData;
var copyright = config.Value<string>("copyright");
var authors = config.Value<JArray>("authors").Values<string>().ToList();
var iconUrl = config.Value<string>("iconUrl");
var tags = config.Value<JArray>("tags").Values<string>().ToList();
var testAppBundleId = config.Value<string>("testAppBundleId");

// Artifacts
var artifactDirectory = config.Value<string>("artifactDirectory");
var packageWhitelist = config.Value<JArray>("packageWhiteList").Values<string>();

var buildSolution = config.Value<string>("solutionFile");
var configuration = "Release";
var simulatorRuntimes = config.Value<JArray>("simulatorRuntimes").Values<string>();
var simulatorDevice = config.Value<string>("simulatorDevice");
var runUnitTests = config.Value<bool>("runUnitTests");
var runSimulatorTests = config.Value<bool>("runSimulatorTests");

// Macros

Func<string> GetMSBuildLoggerArguments = () => {
    return BuildSystem.TeamCity.IsRunningOnTeamCity ? EnvironmentVariable("MsBuildLogger"): null;
};


Func<List<string>, TestResults> GetTestResultsFromLogs = (logItems) => {
	var testResults = new TestResults();
	logItems.Reverse();
	
	foreach (var line in logItems)
    {
        // Unit for Devices = "Tests run: 0 Passed: 0 Failed: 0 Skipped: 0"
        // NUnit for Devices = "Tests run: 2 Passed: 1 Inconclusive: 0 Failed: 1 Ignored: 1
        if (line.Contains("Tests run:"))
        {
            var testLine = line.Substring(line.IndexOf("Tests run:", StringComparison.Ordinal));
            var testArray = Regex.Split(testLine, @"\D+").Where(s => s != string.Empty).ToArray();
            testResults.Run = int.Parse(testArray[0]);
            testResults.Passed = int.Parse(testArray[1]);
            if (testArray.Length == 4)
            {
                testResults.Failed = int.Parse(testArray[2]);
                testResults.Skipped = int.Parse(testArray[3]);
            }
            else
            {
                testResults.Inconclusive = int.Parse(testArray[2]);
                testResults.Failed = int.Parse(testArray[3]);
                testResults.Skipped = int.Parse(testArray[4]);
            }
            break;
        }
    }

	return testResults;
};


Action Abort = () => { throw new Exception("A non-recoverable fatal error occurred."); };
Action<string> TestFailuresAbort = testResult => { throw new Exception(testResult); };
Action NonMacOSAbort = () => { throw new Exception("Running on platforms other macOS is not supported."); };

Action<string, string> unitTestiOSApp = (bundleId, appPath) =>
{
    Information("Shutdown");
    ShutdownAllAppleSimulators();

    var setting = new SimCtlSettings() { ToolPath = FindXCodeTool("simctl") };
    var simulators = ListAppleSimulators(setting);
    var device = simulators.First(x => x.Name == simulatorDevice && simulatorRuntimes.Contains(x.Runtime));
    Information(string.Format("Name={0}, UDID={1}, Runtime={2}, Availability={3}", device.Name, device.UDID,device.Runtime,device.Availability));

    Information("LaunchAppleSimulator");
    LaunchAppleSimulator(device.UDID);
    Thread.Sleep(30 * 1000);

    Information("UninstalliOSApplication");
    UninstalliOSApplication(
        device.UDID, 
        bundleId,
        setting);
	Thread.Sleep(60 * 1000);

    Information("InstalliOSApplication");
    InstalliOSApplication(
        device.UDID,
        appPath,
        setting);
	// Delay to allow simctl install to finish, otherwise you can receive the following error:
	// The request was denied by service delegate (SBMainWorkspace) for reason: 
	Thread.Sleep(30 * 1000);

    Information("TestiOSApplication");
    var testResults = TestiOSApplication(
        device.UDID, 
        bundleId,
        setting);
    Information("Test Results:");
    Information(string.Format("Tests Run:{0} Passed:{1} Failed:{2} Skipped:{3} Inconclusive:{4}", 
					testResults.Run, testResults.Passed, testResults.Failed,testResults.Skipped,testResults.Inconclusive));    

    Information("UninstalliOSApplication");
    UninstalliOSApplication(
        device.UDID, 
        bundleId,
        setting);

    Information("Shutdown");
    ShutdownAllAppleSimulators();

    if (testResults.Run > 0 && testResults.Failed > 0) 
    {
		TestFailuresAbort(string.Format("iOS unit tests failed: {0}", testResults.Failed));
    }
};

Action<string, string> unitTestAndroidApp = (packageId, projectFile) =>
{
	var androidHome = EnvironmentVariable("ANDROID_HOME");

	if(string.IsNullOrEmpty(androidHome)) 
	{
		return;
	}

	Information("Creating Apk package");
	var apk = AndroidPackage(projectFile, true, settings => {
		settings
		.WithProperty("OutputPath", "../" + artifactDirectory)
		.WithProperty("NoWarn", "1591") // ignore missing XML doc warnings
		.WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
		.SetVerbosity(Verbosity.Minimal);
	});

	Information(string.Format("apk: {0}", apk));
	var adbSettings = new AdbToolSettings() {
		SdkRoot = EnvironmentVariable("ANDROID_HOME")
	};

	var devices = AdbDevices(adbSettings);
	var device = devices.FirstOrDefault(m => !string.IsNullOrWhiteSpace(m.Model));

	if(device != null) 
	{
		adbSettings = new AdbToolSettings() {
			SdkRoot = EnvironmentVariable("ANDROID_HOME"),
			Serial = device.Serial
		};

		Information(string.Format("Serial={0}, Model={1}, Product={2}, Device={3}", device.Serial, device.Model, device.Product,device.Device));
		Information(string.Format("Installing {0}", packageId));
		AdbInstall(artifactDirectory + packageId + "-Signed.apk", adbSettings);
		Thread.Sleep(60 * 1000);
	
		Information("Conducting Tests");
		AdbShell(string.Format("am start -n {0}/{1} -c android.intent.category.LAUNCHER", packageId , "com.xunit.runneractivity"), adbSettings);
	
		var logs = AdbLogcat(new AdbLogcatOptions() {
		
		}, "mono-stdout:I *:S", adbSettings);

		Thread.Sleep(60 * 1000);
	
		var testResults = GetTestResultsFromLogs(logs);
		Information("Test Results:");
		Information(string.Format("Tests Run:{0} Passed:{1} Failed:{2} Skipped:{3} Inconclusive:{4}", 
						testResults.Run, testResults.Passed, testResults.Failed,testResults.Skipped,testResults.Inconclusive));    
		
		Information(string.Format("Uninstalling {0}", packageId));
		AdbUninstall(packageId, false, adbSettings);

		
	   if (testResults.Run > 0 && testResults.Failed > 0) 
	   {
			TestFailuresAbort(string.Format("Android unit tests failed: {0}", testResults.Failed));
	   }
	}	


   

};

Action<string> SourceLink = (solutionFileName) =>
{
    GitLink("./", new GitLinkSettings() {
        RepositoryUrl = githubUrl,
        SolutionFileName = solutionFileName,
        ErrorsAsWarnings = true
    });
};

Action<string, string, Exception> WriteErrorLog = (message, identity, ex) => 
{
	if(isTeamCity) 
	{
		teamCity.BuildProblem(message, identity);
		teamCity.WriteStatus(String.Format("{0}", identity), "ERROR", ex.ToString());
		throw ex;
	}
	else {
		throw new Exception(String.Format("task {0} - {1}", identity, message), ex);
	}
};


Func<string, IDisposable> BuildBlock = message => {

	if(BuildSystem.TeamCity.IsRunningOnTeamCity) 
	{
		return BuildSystem.TeamCity.BuildBlock(message);
	}
	
	return null;
	
};

Func<string, IDisposable> Block = message => {

	if(BuildSystem.TeamCity.IsRunningOnTeamCity) 
	{
		BuildSystem.TeamCity.Block(message);
	}

	return null;
};

Action<string,string> build = (solution, buildConfiguration) =>
{
    Information("Building {0}", solution);
	using(BuildBlock("Build")) 
	{			

		MSBuild(solution, settings => {
				settings
				.SetConfiguration(buildConfiguration)
				.WithTarget("restore;ChilliSource_Mobile_Ioc_DryIoc:pack;ChilliSource_Mobile_UI_ReactiveUI_DryIoc:pack")
		        .WithProperty("PackageOutputPath",  MakeAbsolute(Directory(artifactDirectory)).ToString())
			    .WithProperty("Version", nugetVersion.ToString())
			    .WithProperty("Authors",  "\"" + string.Join(" ", authors) + "\"")
			    .WithProperty("Copyright",  "\"" + copyright + "\"")
			    .WithProperty("PackageProjectUrl",  "\"" + githubUrl + "\"")
			    .WithProperty("PackageIconUrl",  "\"" + iconUrl + "\"")
			    .WithProperty("PackageLicenseUrl",  "\"" + licenceUrl + "\"")
			    .WithProperty("PackageTags",  "\"" + string.Join(" ", tags) + "\"")
			    .WithProperty("PackageReleaseNotes",  "\"" +  string.Format("{0}/releases", githubUrl) + "\"")
				.WithProperty("NoWarn", "1591") // ignore missing XML doc warnings
				.WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors.ToString())
				.SetVerbosity(Verbosity.Minimal)
				.SetNodeReuse(false);

				var msBuildLogger = GetMSBuildLoggerArguments();
			
				if(!string.IsNullOrEmpty(msBuildLogger)) 
				{
					Information("Using custom MSBuild logger: {0}", msBuildLogger);
					settings.ArgumentCustomization = arguments =>
					arguments.Append(string.Format("/logger:{0}", msBuildLogger));
				}
			});

		
    };		

};

///////////////////////////////////////////////////////////////////////////////
// SETUP / TEARDOWN
///////////////////////////////////////////////////////////////////////////////
Setup((context) =>
{
    Information("Building version {0} of ChilliSource.Mobile. (isTagged: {1})", informationalVersion, isTagged);

		if (isTeamCity)
		{
			Information(
					@"Environment:
					 PullRequest: {0}
					 Build Configuration Name: {1}
					 TeamCity Project Name: {2}
					 Branch: {3}",
					 isPullRequest,
					 buildConfName,
					 projectName,
					 branch
					);
        }
        else
        {
             Information("Not running on TeamCity");
        }

         CleanDirectories(artifactDirectory);
});

Teardown((context) =>
{
    // Executed AFTER the last task.
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////

Task("Build")
	.IsDependentOn("AddLicense")
    .Does (() =>
{
    build(buildSolution, configuration);
})
.OnError(exception => {
	WriteErrorLog("Build failed", "Build", exception);
});


Task("AddLicense")
	.WithCriteria(() => shouldAddLicenseHeader)
	.Does(() =>{
		var command = isRunningOnWindows ? "sh" : "./license-header-cmd.sh";
		var settings = isRunningOnWindows ? new ProcessSettings { Arguments = "-c \"./license-header-cmd.sh\"", RedirectStandardError = true, RedirectStandardOutput = true } : new ProcessSettings { RedirectStandardError = true, RedirectStandardOutput = true };
		var process  = StartAndReturnProcess(command, settings);		
		process.WaitForExit();

		if (process.GetExitCode() != 0){
			throw new Exception("Adding license failed.");
		}
	})
	.ReportError(exception =>{
		Information("Make sure the bash (sh) directory is set in your environment path.");
	});


var testdll = config.Value<string>("testProjectDll");
Task("RunUnitTests")
    .IsDependentOn("Build")
    .WithCriteria(() => runUnitTests)
    .WithCriteria(() => !isRunningOnUnix)
    .Does(() =>
{
	Information("Running Unit Tests for {0}", buildSolution);
	using(BuildBlock("RunUnitTests")) 
	{
		XUnit2(testdll, new XUnit2Settings {
			OutputDirectory = artifactDirectory,
            XmlReportV1 = false,
            NoAppDomain = true
		});
	};
});


Task("RunSimulatorUnitTests")
   .IsDependentOn("Build")
   .WithCriteria(() => runSimulatorTests)
  .Does (() =>
{
	// this should be running on MacOS
	if(isRunningOnUnix) {
		Information("Running iOS Simulator Unit Tests for {0}", project);
		using(BuildBlock("RuniOSSimulatorUnitTests")) 
		{
		    unitTestiOSApp(
		        testAppBundleId,
		        config.Value<string>("iosTestAppPath")
		    );
		}
	}

	Information("Running Android Emulator Unit Tests for {0}", project);
	var projectFile = config.Value<string>("androidTestAppPath");

	
	using(BuildBlock("RunAndroidEmulatorUnitTests")) 
	{
		unitTestAndroidApp(
			testAppBundleId,
			projectFile
		);
	}
})
.OnError(exception => {
	WriteErrorLog("simulator " + exception.Message, "RunSimulatorUnitTests", exception);
});


Task("PublishPackages")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("RunSimulatorUnitTests")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .Does (() =>
{
	using(BuildBlock("Package"))
	{
		string apiKey;
		string source;

		if (isReleaseBranch && !isTagged)
		{
			// Resolve the API key.
			apiKey = EnvironmentVariable("MYGET_APIKEY");
			if (string.IsNullOrEmpty(apiKey))
			{
				throw new Exception("The MYGET_APIKEY environment variable is not defined.");
			}

			source = EnvironmentVariable("MYGET_SOURCE");
			if (string.IsNullOrEmpty(source))
			{
				throw new Exception("The MYGET_SOURCE environment variable is not defined.");
			}
		}
		else 
		{
			// Resolve the API key.
			apiKey = EnvironmentVariable("NUGET_APIKEY");
			if (string.IsNullOrEmpty(apiKey))
			{
				throw new Exception("The NUGET_APIKEY environment variable is not defined.");
			}

			source = EnvironmentVariable("NUGET_SOURCE");
			if (string.IsNullOrEmpty(source))
			{
				throw new Exception("The NUGET_SOURCE environment variable is not defined.");
			}
		}



		// only push whitelisted packages.
		foreach(var package in packageWhitelist)
		{
			// only push the package which was created during this build run.
			var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

			// Push the package.
			NuGetPush(packagePath, new NuGetPushSettings {
				Source = source,
				ApiKey = apiKey
			});
		}

	};

  
})
.OnError(exception => {
	WriteErrorLog("publishing packages failed", "PublishPackages", exception);
});

Task("CreateRelease")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("RunSimulatorUnitTests")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => !isTagged)
    .WithCriteria(() => isRunningOnWindows)
    .Does (() =>
{
	using(BuildBlock("CreateRelease"))
	{
		var username = EnvironmentVariable("GITHUB_USERNAME");
		if (string.IsNullOrEmpty(username))
		{
			throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
		}

		var token = EnvironmentVariable("GITHUB_TOKEN");
		if (string.IsNullOrEmpty(token))
		{
			throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
		}

		GitReleaseManagerCreate(username, token, githubOwner, githubRepository, new GitReleaseManagerCreateSettings {
			Milestone         = majorMinorPatch,
			Name              = majorMinorPatch,
			Prerelease        = true,
			TargetCommitish   = "master"
		});
	};

})
.OnError(exception => {
	WriteErrorLog("creating release failed", "CreateRelease", exception);
});

Task("PublishRelease")
    .IsDependentOn("RunUnitTests")
    .IsDependentOn("RunSimulatorUnitTests")
    .WithCriteria(() => !local)
    .WithCriteria(() => !isPullRequest)
    .WithCriteria(() => isRepository)
    .WithCriteria(() => isReleaseBranch)
    .WithCriteria(() => isTagged)
    .WithCriteria(() => isRunningOnWindows)
    .Does (() =>
{
	using(BuildBlock("PublishRelease"))
	{
		var username = EnvironmentVariable("GITHUB_USERNAME");
		if (string.IsNullOrEmpty(username))
		{
			throw new Exception("The GITHUB_USERNAME environment variable is not defined.");
		}

		var token = EnvironmentVariable("GITHUB_TOKEN");
		if (string.IsNullOrEmpty(token))
		{
			throw new Exception("The GITHUB_TOKEN environment variable is not defined.");
		}

		// only push whitelisted packages.
		foreach(var package in packageWhitelist)
		{
			// only push the package which was created during this build run.
			var packagePath = artifactDirectory + File(string.Concat(package, ".", nugetVersion, ".nupkg"));

			GitReleaseManagerAddAssets(username, token, githubOwner, githubRepository, majorMinorPatch, packagePath);
		}

		GitReleaseManagerClose(username, token, githubOwner, githubRepository, majorMinorPatch);
	}; 
})
.OnError(exception => {
	WriteErrorLog("updating release assets failed", "PublishRelease", exception);
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Default")
    .IsDependentOn("CreateRelease")
    .IsDependentOn("PublishPackages")
    .IsDependentOn("PublishRelease")
    .Does (() =>
{

});


//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
