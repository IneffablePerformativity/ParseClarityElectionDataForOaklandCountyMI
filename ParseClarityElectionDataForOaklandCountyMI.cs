/*
 * ParseClarityElectionDataForOaklandCountyMI.cs
 *
 * which code and results I will archive at:
 * https://github.com/IneffablePerformativity
 * 
 * "To the extent possible under law, Ineffable Performativity has waived all copyright and related or neighboring rights to
 * The C# program Georgia2020ElectionFraud.cs and resultant outputs.
 * This work is published from: United States."
 * 
 * This work is offered as per license: http://creativecommons.org/publicdomain/zero/1.0/
 * 
 * 
 * This application has mined out and presents solid proof of 2020 POTUS ELECTION FRAUD in named locality.
 * WOW!!! BLAZING HOT Michigan Fraud Results: I call this evidence tight: 1.5% of totalBallots was added to Joe across the entire county!
 * The Democrat Vote PPM Shift (Potus Dem minus Other Dems) in all 506 localities of Oakland County Michigan is: mean=15140, stdDev=2531.
 * 
 * Goal: Parsing the "Clarity" type of 2020 Election data for Oakland County Michigan.
 * via https://www.oakgov.com/clerkrod/elections/Pages/default.aspx
 * via https://results.enr.clarityelections.com/MI/Oakland/105840/
 * Where I manually downloaded detailxml.zip, extracted detail.xml,
 * and renamed it: OaklandCountyMI-detail.xml
 * 
 * to demonstrate any inverse republicanism::trump relationship
 * as was described for Milwaukee County Wisconsin in an article at:
 * https://www.thegatewaypundit.com/2020/11/caught-part-3-impossible-ballot-ratio-found-milwaukee-results-change-wisconsin-election-30000-votes-switched-president-trump-biden/
 * 
 * 
 * This application comes on the heels of my similar successful app,
 * which was also saved there on github.com/IneffablePerformativity:
 * ParseMilwaukeeCountyWiVotes.cs
 * That app showed a very clear 3% skew of POTUS race from lower races.
 * 
 * 
 * I am cloning an app that did the CLARITY data for state of Georgia.
 * But that data did NOT show POTUS race to be skewed from lower races,
 * so I did not proceed to the plotting step therein. Maybe this time.
 * 
 * 
 * The least grained item in State of Georgia was called a County.
 * The least grained item in Oakland County MI is called a Precinct.
 * One cannot compare at higher grained level Contests to one another
 * because they do not consist of all the very same individual voters.
 * 
 * 
 * Georgia did NOT give me TotalBallots cast per county.
 * So I approximated it as the sum of all POTUS choices.
 * 
 * 
 * Georgia had 4 vote types ~(MailIn/ElectionDay/Advance/Provisional).
 * Oakland County MI has only 2 vote types (under Choice):
 * Choice.VoteType = "Election"
 * Choice.VoteType = "Absentee"
 * Counting VoteTypes separately created big code bloat I did not use.
 * 
 * New PLAN: I will simplify and more generally name data structures:
 * Contest[] -> Choice[] -> Grain[] -> SumVotes (across all VoteType)
 * 
 * Georgia had "(Dem)" or "(Rep)" in Choice text (name of candidate).
 * Here, Choice has OPTIONAL attribute "party": "DEM", "REP", others.
 * 
 * Here is a new concept: Contest may be "Straight Party Ticket"
 * Which I see alongside normal Contest names like:
 * "Electors of President and Vice-President of the United States"
 * "United States Senator"
 * "Representative in Congress 8th District"
 * "Representative in State Legislature 26th District"
 * and many lower contest names.
 * 
 * Planning: Such votes can credit POTUS and US SENATORS,
 * but applying to local races would be more complicated.
 * Also "Representative in Congress..." is Federal level,
 * 
 * 
 * Here is a new concept: Contest.VoteType  = "Undervotes"
 * Here is a new concept: Contest.VoteType  = "Overvotes"
 * Which pair are seen in all contests, before multiple "Choice" nodes;
 * Note the reuse of same xml name VoteType at this higher DOM level.
 * 
 * Wikipedia:
 * - An overvote occurs when one votes for more than the maximum number of selections allowed in a contest.
 * The result is a spoiled vote which is not included in the final tally.
 * - An undervote occurs when the number of choices selected by a voter in a contest is less than the maximum number allowed for that contest or when no selection is made for a single choice contest.
 * - Undervotes combined with overvotes (known as residual votes) can be an academic indicator in evaluating the accuracy of a voting system when recording voter intent.
 * 
 * 
 * 
 * Prior App for GEORGIA comments:
 * 
 * Oh, but I had used the totalBallotsCast, also totalRegistered by Ward;
 * Whereas these Georgia results do not share that information by county.
 * I will estimate totalBallotsCast in county = POTUS (Rep)+(Dem)+(Lib);
 * I.e., as if nobody had voted without marking any presidential choice.
 * 
 * The smallest granuarity unit is the COUNTY (as was WARD in prior app).
 * Counties are the plot independent variable, ordered by "republicanism".
 * I wonder, should I include POTUS race in the measure of republicanism?
 * That might smooth better than only averaging the contested lower races.
 * 
 * 
 * 
 * On to the task...
 */


using System;
using System.Collections.Generic;
//using System.Drawing; // Must add a reference to System.Drawing.dll
//using System.Drawing.Imaging; // for ImageFormat
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Xml;


namespace ParseClarityElectionDataForOaklandGrainMI
{
	
	class Choice // Candidate
	{
		public Choice(string party)
		{
			this.party = party;
		}
		public string party = "";
		public int votes = 0;
	}
	
	class Contest // Race
	{
		public Dictionary<string,Choice> choices = new Dictionary<string,Choice>();
	}
	
	class Grain // Precinct, County, Ward, etc.
	{
		public Grain(string locality)
		{
			this.locality = locality;
		}
		public string locality = "";
		public Dictionary<string,Contest> contests = new Dictionary<string,Contest>();
	}
	
	class ShrodingersCat
	{
		public int rep;
		public int dem;
		public int etc;
	}

	class Program
	{
		static Dictionary<string,Grain> grains = new Dictionary<string,Grain>();
		
		// inputting phase
		
		static string projectDirectoryPath = @"C:\A\SharpDevelop\ParseClarityElectionDataForOaklandCountyMI";
		static string inputXmlFileName = "OaklandCountyMI-detail.xml"; // sitting there

		const string GrainTag = "Precinct"; // XML tag name e.g., Precinct, County, Ward, etc.
		

		// Analysis Phase
		
		const int ballotsCast = 775379; // Empirically, from this current input file

		
		
		// outputting phase

		static string DateTimeStr = DateTime.Now.ToString("yyyyMMddTHHmmss");
		
		// Log outputs exploratory, debug data:
		static string logFilePath = @"C:\A\" + DateTimeStr + "_ParseClarityElectionDataForOaklandGrainMI_log.txt";
		static TextWriter twLog = null;

		// a favorite output idiom
		static void say(string msg)
		{
			if(twLog != null)
				twLog.WriteLine(msg);
			else
				Console.WriteLine(msg);
		}

		// Csv outputs voting data to visualize in Excel
		static string csvFilePath = @"C:\A\" + DateTimeStr + "_ParseClarityElectionDataForOaklandGrainMI_csv.csv";
		static TextWriter twCsv = null;

		static void csv(string msg)
		{
			if(twCsv != null)
				twCsv.WriteLine(msg);
		}

		static Regex regexNonAlnum = new Regex(@"\W", RegexOptions.Compiled);

		// Png outputs my own bitmap for fine grained control of data display:
		// static string pngFilePath = @"C:\A\" + DateTimeStr + "_ParseClarityElectionDataForOaklandGrainMI_png.png";

		
		public static void Main(string[] args)
		{
			Console.WriteLine("Hello World!");
			
			// TODO: Implement Functionality Here
			
			using(twLog = File.CreateText(logFilePath))
				using(twCsv = File.CreateText(csvFilePath))
			{
				try { doit(); } catch(Exception e) {say(e.ToString());}
			}

			Console.Write("Press any key to continue . . . ");
			Console.ReadKey(true);
		}
		
		static void doit()
		{
			// Phase one -- Inputting data
			
			inputDetailXmlFile();

			// diagnosticDump();
			

			// Phase two -- Analyze data
			
			LetsRaceTowardTheDesideratum();

			
			// Phase three -- Output data
			
			if(twCsv != null)
				csv("ppmOrdering,ppmRepPotus,ppmDemPotus,1M-ppmDemPotus,ppmRepOther,ppmDemOther,1M-ppmDemOther,ppmRepShift,1M-ppmDemShift,totalBallots,locality");

			OutputTheCsvResults();
		}

		
		static void inputDetailXmlFile()
		{
			XmlDocument doc = new XmlDocument();
			doc.Load(Path.Combine(projectDirectoryPath,inputXmlFileName));

			XmlNode root = doc.DocumentElement;
			
			// recursive descent
			
			XmlNodeList xnlContests = root.SelectNodes("Contest");
			foreach(XmlNode xnContest in xnlContests)
			{
				string contest = xnContest.Attributes["text"].Value;
				
				// Having just run through all to see the lay of it,
				// Only process contest of interest to my statistic.
				
				switch(contest)
				{
						// OMIT THIS LINE THAT JUST DILUTES ANy SYMPTOM:
						// case "Straight Party Ticket":
					case "Electors of President and Vice-President of the United States":
					case "United States Senator":
					case "Representative in Congress 8th District":
					case "Representative in Congress 9th District":
					case "Representative in Congress 11th District":
					case "Representative in Congress 14th District":
						break;
					default:
						continue;
				}

				XmlNodeList xnlChoices = xnContest.SelectNodes("Choice");
				foreach(XmlNode xnChoice in xnlChoices)
				{
					string choice = xnChoice.Attributes["text"].Value;
					string party = "";
					try { party = xnChoice.Attributes["party"].Value; } catch(Exception) { };


					// Don't leave out the other choices,
					// need them for total votes in grain
					//switch(party)
					//{
					//	case "DEM":
					//	case "REP":
					//		break;
					//	default:
					//		continue;
					//}

					// 1. Real votes
					XmlNodeList xnlGrains = xnChoice.SelectNodes("VoteType/" + GrainTag);
					foreach(XmlNode xnGrain in xnlGrains)
					{
						string grain = xnGrain.Attributes["name"].Value;
						string votes = xnGrain.Attributes["votes"].Value;
						
						ponderInput(contest, choice, party, grain, votes);
					}
				}

				// Having seen it, now not needed
				//
				//{
				//	// 2. Residual votes
				//	// <VoteType name="Undervotes"...
				//	XmlNodeList xnlGrains = xnContest.SelectNodes("VoteType[@name='Overvotes' or @name='Undervotes']/" + GrainTag);
				//	foreach(XmlNode xnGrain in xnlGrains)
				//	{
				//		string grain = xnGrain.Attributes["name"].Value;
				//		string votes = xnGrain.Attributes["votes"].Value;
				//
				//		ponderInput(contest, "Residual", "", grain, votes);
				//	}
				//}
			}
			// Wow. That was way easier.
		}
		
		static void ponderInput(string contest, string choice, string party, string grain, string votes)
		{
			// say(contest + "::" + choice + "::" + party + "::" + grain + "::" + votes);
			
			int nVotes = int.Parse(votes);
			
			// recursive ascent
			
			Grain thisGrain = null;
			if(grains.ContainsKey(grain))
				thisGrain = grains[grain];
			else
				grains.Add(grain, thisGrain = new Grain(grain));
			
			Contest thisContest = null;
			if(thisGrain.contests.ContainsKey(contest))
				thisContest = thisGrain.contests[contest];
			else
				thisGrain.contests.Add(contest, thisContest = new Contest());
			
			Choice thisChoice = null;
			if(thisContest.choices.ContainsKey(choice))
				thisChoice = thisContest.choices[choice];
			else
				thisContest.choices.Add(choice, thisChoice = new Choice(party));
			
			thisChoice.votes += nVotes;
			
			// Wow. Another easy peasy.
		}
		
		/*
		 * I think having made the effort to parse and count Residual counts,
		 * all contests->...->grains->votes should sum to the identical ballotsCast.
		 * 
		 * <VoterTurnout totalVoters="1035172" ballotsCast="775379" voterTurnout="74.90">
		 * 
		 * No, I think not now:
		 * Some local races have fewer of the statewide ballots,
		 * but I did get some lines to sum up within a Grain:
		 * 
		 * input, under TotalVotes:
		 * <Precinct name="Addison Township, Precinct 1" totalVoters="1930" ballotsCast="1590" ...
		 * 
		 * versus output:
		 * Addison Township, Precinct 1::Straight Party Ticket::1590
		 * Addison Township, Precinct 1::Electors of President and Vice-President of the United States::1590
		 * Addison Township, Precinct 1::United States Senator::1590
		 * 
		 * however, I see some near-multiples and multiples too:
		 * Addison Township, Precinct 1::Member of the State Board of Education::3179
		 * Addison Township, Precinct 1::Regent of the University of Michigan::3180
		 * 
		 * So I think my diagnostic failed to sum-up to a high enough level...
		 * 
		 * Fixed it.
		 * 
		 * Did not snag all of Federals; Many others are don't cares:
		 * GOOD TOTAL: Straight Party Ticket
		 * GOOD TOTAL: Electors of President and Vice-President of the United States
		 * GOOD TOTAL: United States Senator
		 * hand work:
		 * Line 1: Representative in Congress 8th District = 164088
		 * Line 26: Representative in Congress 9th District = 137999
		 * Line 49: Representative in Congress 11th District = 293394
		 * Line 156: Representative in Congress 14th District = 179898
		 * 164088 + 137999 + 293394  + 179898 = 775379, matches ballotsCast = 775379
		 */

		static void diagnosticDump()
		{
			// because of the inverted order of grain<-->contest, sum up:
			Dictionary<string,int> contestSums = new Dictionary<string, int>();

			foreach(KeyValuePair<string,Grain>kvp1 in grains)
			{
				string g = kvp1.Key;
				Grain grain = kvp1.Value;
				foreach(KeyValuePair<string,Contest>kvp2 in grain.contests)
				{
					string c = kvp2.Key;
					Contest contest = kvp2.Value;
					int sumVotes = 0;
					foreach(KeyValuePair<string,Choice>kvp3 in contest.choices)
					{
						string n = kvp3.Key;
						Choice choice = kvp3.Value;
						sumVotes += choice.votes;
					}
					if(contestSums.ContainsKey(c))
						contestSums[c] += sumVotes;
					else
						contestSums.Add(c, sumVotes);
				}
			}
			
			int nGood = 0;
			int nBad = 0;
			foreach(KeyValuePair<string,int>kvp in contestSums)
			{
				if(kvp.Value == ballotsCast)
				{
					// say("GOOD TOTAL: " + kvp.Key); // see which 15?
					nGood++;
				}
				else
				{
					// say(kvp.Key + " = " + kvp.Value); // How bad?
					nBad++;
				}
			}
			// say("nGood = " + nGood);
			// say("nBad = " + nBad);
			//nGood = 15
			//nBad = 260
			// I'll take that as a big win
		}
		

		// This will collect output lines to sort for csv.
		// I will re-run the pq if I choose to draw graph.
		
		static List<string> pq = new List<string>(); // "priority queue" I like to call it

		
		// My God! Thank You, My God!
		// The Excel plot reveals a VERY STRAIGHT LINE
		// revealing the algorithm favoring Joe Biden.
		// So that I must see and report mean, std dev.

		static List<int> DemShifts = new List<int>();
		
		
		// probably a lot like the diagnostic dump loop
		
		static void LetsRaceTowardTheDesideratum()
		{
			StringBuilder sb = new StringBuilder();
			
			// Grains are plotted along the Abscissa, the X axis.
			// Each outer loop can prepare one csv/plot data out.
			// That is because I already learned the ballotsCast.

			// I will learn contest long before I learn party.

			ShrodingersCat [] four = new ShrodingersCat[4];

			four[0] = new ShrodingersCat(); // Straight
			four[1] = new ShrodingersCat(); // President
			four[2] = new ShrodingersCat(); // Senate
			four[3] = new ShrodingersCat(); // Congress

			int which = 0;
			
			foreach(KeyValuePair<string,Grain>kvp1 in grains)
			{
				string g = kvp1.Key; // county, ward, precinct name
				Grain grain = kvp1.Value;

				// Middle loop splits up 2 contest types: (POTUS vs. LOWER)
				// Well, actually, into the four cats I created just above.

				foreach(KeyValuePair<string,Contest>kvp2 in grain.contests)
				{
					string c = kvp2.Key;
					Contest contest = kvp2.Value;

					switch(c)
					{
						case "Straight Party Ticket":
							// I should add to all possible races.
							// These will only dilute the symptom.
							which = 0;
							break;

						case "Electors of President and Vice-President of the United States":
							which = 1;
							break;

						case "United States Senator":
							which = 2;
							break;

						case "Representative in Congress 8th District":
						case "Representative in Congress 9th District":
						case "Representative in Congress 11th District":
						case "Representative in Congress 14th District":
							which = 3;
							break;
					}

					// Inner loop splits up 2 choice types: (REP vs. DEM).

					foreach(KeyValuePair<string,Choice>kvp3 in contest.choices)
					{
						string n = kvp3.Key;
						Choice choice = kvp3.Value;
						
						switch(choice.party)
						{
							case "DEM":
								four[which].dem += choice.votes;
								break;
							case "REP":
								four[which].rep += choice.votes;
								break;
							default:
								four[which].etc += choice.votes;
								break;
						}
					}
				}

				// build an output line
				
				//fyi: csv("ppmOrdering,ppmRepPotus,ppmDemPotus,1M-ppmDemPotus,ppmRepOther,ppmDemOther,1M-ppmDemOther,ppmRepShift,1M-ppmDemShift,totalBallots,locality");
				
				// sum up VOTES:
				int RepPotus = four[0].rep + four[1].rep;
				int DemPotus = four[0].dem + four[1].dem;
				int EtcPotus = four[0].etc + four[1].etc;
				int totalBallots = RepPotus + DemPotus + EtcPotus;

				int RepOther = four[0].rep + (four[2].rep + four[3].rep) / 2;
				int DemOther = four[0].dem + (four[2].dem + four[3].dem) / 2;
				
				
				int ppmRepPotus = (int)(1000000L * RepPotus / totalBallots);
				int ppmDemPotus = (int)(1000000L * DemPotus / totalBallots);

				int ppmRepOther = (int)(1000000L * RepOther / totalBallots);
				int ppmDemOther = (int)(1000000L * DemOther / totalBallots);
				
				int ppmRepShift = ppmRepPotus - ppmRepOther;
				int ppmDemShift = ppmDemPotus - ppmDemOther;
				
				DemShifts.Add(ppmDemShift); // for later mean, std dev

				// This ordering method follows (only Other, of only Rep) no Dem, no Potus,
				// as Original Milwaukee article says shifts proportional to Republicanism.
				
				int ppmOrdering = ppmRepOther;

				sb.Clear();
				sb.Append(ppmOrdering.ToString().PadLeft(7)); sb.Append(',');

				sb.Append(ppmRepPotus.ToString().PadLeft(7)); sb.Append(',');
				sb.Append(ppmDemPotus.ToString().PadLeft(7)); sb.Append(',');
				sb.Append((1000000 - ppmDemPotus).ToString().PadLeft(7)); sb.Append(',');

				sb.Append(ppmRepOther.ToString().PadLeft(7)); sb.Append(',');
				sb.Append(ppmDemOther.ToString().PadLeft(7)); sb.Append(',');
				sb.Append((1000000 - ppmDemOther).ToString().PadLeft(7)); sb.Append(',');

				sb.Append(ppmRepShift.ToString().PadLeft(7)); sb.Append(',');
				sb.Append((1000000 - ppmDemShift).ToString().PadLeft(7)); sb.Append(',');

				sb.Append(totalBallots.ToString().PadLeft(7)); sb.Append(',');

				sb.Append(regexNonAlnum.Replace(grain.locality, ""));

				pq.Add(sb.ToString());
			}
		}
		
		static void OutputTheCsvResults()
		{
			pq.Sort();
			
			foreach(string line in pq)
				csv(line);
			
			// Special output needed for OaklandCountyMI:
			int sum = 0;
			foreach(int i in DemShifts)
				sum += i;
			double mean = (double)sum / DemShifts.Count;
			
			double sumSquares = 0.0;
			foreach(int i in DemShifts)
				sumSquares += (mean-i)*(mean-i);
			double stdDev = Math.Sqrt(sumSquares / DemShifts.Count);
			
			int imean = (int)Math.Round(mean);
			int istdDev = (int)Math.Round(stdDev);
			
			say("The Democrat Vote PPM Shift (Potus Dem minus Other Dems) in all " + DemShifts.Count + " localities of Oakland County Michigan is: mean=" + imean + ", stdDev=" + istdDev + ".");

			// Wow. I call this evidence tight: 1.5% of totalBallots was added to Joe across the entire county!
			// The Democrat Vote PPM Shift (Potus Dem minus Other Dems) in all 506 localities of Oakland County Michigan is: mean=15140, stdDev=2531.

		}
	}
}
