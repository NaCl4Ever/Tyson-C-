﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Timers;
using System.IO;
using edu.stanford.nlp.parser;
using java.util;
using edu.stanford.nlp.ie.crf;
using edu.stanford.nlp.pipeline;
using edu.stanford.nlp.util;
using Microsoft.Win32;
using System.Windows.Forms;
using System.Security;
using System.Security.Cryptography;

using System.Web;


namespace basicGUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Variable Declartations 
        /// currfileList: List containing all current file pathways for the batch you are working on
        /// sentences: Contains sentences of the current content being displayed. 
        /// WordBank:  Used only during Testing, this is represents the required words. 
        /// testWordBank: 
        /// temporaryBank:
        /// printedBank: Sentences that will be printed in the current visual listbox of the word bank.
        /// currentMode: Will be updated to show what the mode is, and also used to update the GUI with the current mode. Sometimes used in checks
        /// currFile:  Points to the current file you are accessing regardless of mode
        /// testLoaded: Lets you know if a test has been loaded currently
        /// paperPass: determines whether or not a paper has past the current
        /// testModeisEnabled: True if you are evaluating tests, false if you are evaluating assignments
        /// currentSentCount = Provides the number of sentences currently in the content shown.
        /// currentWordCount = Provides the number of words currently in the content shown. 
        /// testSentencesMin = Minimum Number of sentences for a test paper to pass.
        /// testWordsMin = Minimum Number of words for a sentence to be considered a sentence.
        /// myTimer = your timer
        /// 
        /// </summary>
        List<string> currFileList = new List<string>(0);
        List<string> sentences = new List<string>(0);
        List<string> wordBank = new List<string>(0);
        List<string> testWordBank = new List<string>(0);
        List<string> temporaryBank = new List<string>(0);
        List<string> printedBank = new List<string>(0);
        

        string currentMode = "";
        string currfile = null;

        Boolean testLoaded = false;
        Boolean paperPass = false;
        Boolean testModeIsEnabled = false;

        int currentSentCount = 0;
        int currentWordCount = 0;
        int testSentencesMin, testWordsMin;
        
       
        static System.Timers.Timer myTimer;
        // Prepares View
       public MainWindow()
        {
            InitializeComponent();
        }

        //Currently Refreshes forms for Testing purposes, will be removed in final release
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Update();
            
        }

        //If User types or edits the text in the content area all forms are updated with their current values
        private void Content_TextChanged(object sender, TextChangedEventArgs e)
        {
            try{
                Update();
            }
                catch(NullReferenceException ex){
                   
                }
            
        }
        /// <summary>
        /// Updates all of the current values possible to update based of the current content being displayed. 
        /// Along with this updates the text labels and modes accordingly 
        /// </summary>
        private void Update()
        {
            
            List<string> sentencesReturned = new List<string>(0);
            string Essay = Content.Text;
            getSentences(Essay);
            generateSentenceList();
            printedBank.Clear();
            foreach(string targetWord in temporaryBank){

                sentencesReturned = sentenceReturn(targetWord, sentences);
                if (sentencesReturned.Count == 0)
                {
                    
                }
                foreach (string returnedSent in sentencesReturned)
                {
                    
                    if (!printedBank.Contains(returnedSent))
                    { printedBank.Add(returnedSent); }
                
                }

            }
            loadedBank.ItemsSource = printedBank;
            loadedBank.Items.Refresh();
            wordsLabel.Content = UniqueWords(Content.Text);
            sentenceLabel.Content = currentSentCount.ToString();
            
            
            
        }
        /// <summary>
        /// Tokenizes the text block based off of whitespace. Assumes all tokens are valid words. Misspelled or correct
        /// </summary>
        /// <param name="inTextBlock"></param>
        private void wordCount(string inTextBlock)
        {
            //Removes Excess White space
            inTextBlock = inTextBlock.Trim();
            //Breaks everything into tokens based off white space present in the text
            List<string> words = inTextBlock.Split(' ').ToList();
            //Removes any empty values if a user has large gaps of white space. Thus preventing an erroneous reading
            if (words.Contains(""))
            {
                words.RemoveAll(delegate(string word){return word.Equals("");});
                currentWordCount = words.Count;
            }
            else
            {currentWordCount = words.Count;}

        }
        /// <summary>
        ///  Breaks inTextBlock into sentences, spliting based on the following characters "." "?" "!" 
        /// </summary>
        /// <param name="inTextBlock"></param>
        private void getSentences(string inTextBlock)
        {
            string[] characters = inTextBlock.Split(' ');
            int increment = 0;
            int startCut = 0;
            inTextBlock = inTextBlock.Trim();
            for (int begin = 0; begin <= inTextBlock.Length; begin++)
            {

                if (inTextBlock.Substring(startCut, begin - startCut).Contains(".") || inTextBlock.Substring(startCut, begin - startCut).Contains("?") || inTextBlock.Substring(startCut, begin - startCut).Contains("!"))
                {                  
                    increment++;
                    startCut = begin;
                }
            }
            currentSentCount = increment;
        }
        /// <summary>
        ///     Breaks inTextBlock into sentences, spliting based on the following characters "." "?" "!" 
        ///     Will only increment if the sentences have the required amount of words. 
        /// </summary>
        /// <param name="inTextBlock"></param>
        /// <param name="reqLength"></param>
        private void getSentences(string inTextBlock, int reqLength)
        {
            string[] characters = inTextBlock.Split(' ');

            int increment = 0;
            int startCut = 0;

            inTextBlock = inTextBlock.Trim();
            for (int begin = 0; begin <= inTextBlock.Length; begin++)
            {

                if (inTextBlock.Substring(startCut, begin - startCut).Contains(".") || inTextBlock.Substring(startCut, begin - startCut).Contains("?") || inTextBlock.Substring(startCut, begin - startCut).Contains("!"))
                {

                    if (inTextBlock.Substring(startCut, begin - startCut).Split(' ').Length >= reqLength)
                    { 
                        increment++;
                    }
                    startCut = begin;
                }



            }




            currentSentCount = increment;
        }
        /// <summary>
        /// Using the dictionary functionality words are checked to see if they are contained inside the dictionary if so that value is incremented.
        /// If not they are added. Thus the dictionary will have a quick look up and evaluation and gives an accurate result. 
        /// </summary>
        /// <param name="inTextBlock"></param>
        /// <returns></returns>
        private int UniqueWords(string inTextBlock)
        {
            Dictionary<string, int> dictionary =  new Dictionary<string, int>();
            string[] words = inTextBlock.Split(' ');
            foreach (string word in words)
            {
                if (dictionary.ContainsKey(word))
                {
                    dictionary[word] = dictionary[word]++;
                }
                else {
                    dictionary.Add(word, 1);
                }
            }
            return dictionary.Count;
        }
        /// <summary>
        /// Searchs a list for a target word, it checks if any entries within the search list have the target word then adds them to a return list. 
        /// </summary>
        /// <param name="targetWord"></param>
        /// <param name="searchList"></param>
        /// <returns></returns>
        private List<string> sentenceReturn(string targetWord, List<string> searchList)
        {
            
            List<string> returnList = new List<string>(0);
            foreach (string currSentence in searchList)
            {
                if (currSentence.Contains(targetWord))
                    {   
                        returnList.Add(currSentence);
                    }
            }
            return returnList;
        }
        /// <summary>
        /// Creates a list of sentences so that they are preserved. Also increments the count of how many there are. 
        /// </summary>
        private void generateSentenceList()
        {
            sentences.Clear();
            int startCut = 0;
            string contentBlock = Content.Text.Trim().ToLower();
            for (int begin = 0; begin <= contentBlock.Length; begin++)
            {
                if (contentBlock.Substring(startCut, begin - startCut).Contains(".") || contentBlock.Substring(startCut, begin - startCut).Contains("?") || contentBlock.Substring(startCut, begin - startCut).Contains("!"))
                {
                    sentences.Add(contentBlock.Substring(startCut, begin - startCut));
                    startCut = begin;
                }
            }
        }
        /// <summary>
        /// Updates the GUI label of the current mode and also updates the value at currentMode to the input of currMode
        /// </summary>
        /// <param name="currMode"></param>
        private void setMode(string currMode)
        {
            currentMode = currMode;
            Action act = () => { modeLabel.Content = currMode; };
            Content.Dispatcher.Invoke(act);
        }
        /// <summary>
        /// Loads a single file, provide a dialog then selects from there if not returns nothing. Also runs update to keep content up to date. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadFile(object sender, RoutedEventArgs e)
        {
            //string fileText = System.IO.File.ReadAllText(@"F:\Users\Jordan\Documents\Projects\test.txt");
            //Content.Text = fileText;

            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.Filter = "Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            bool? userClickedOK = openFileDialog1.ShowDialog();
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                testModeIsEnabled = false;
                CurrFileName.Text = openFileDialog1.SafeFileName;                    
                
                String fileText = System.IO.File.ReadAllText(openFileDialog1.FileName);
                currfile = openFileDialog1.FileName;
                if (fileText.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileText.IndexOf("-:Tyson Results:-");
                     fileText = fileText.Substring(0, endPoint);
                
                
                
                }
                Content.Text = fileText;
                filePosition.Text = "N/A";
                Update();
                setMode("Single Evaluation");
                currFileList.Clear();
            }
        }
        /// <summary>
        /// Currently not in use, but will give you the number of times a word shows up in a target text in this case referred to as textToView
        /// </summary>
        /// <param name="targetWord"></param>
        /// <param name="textToView"></param>
        /// <returns>Frequnecy of targetWord in textToView</returns>
        public int frequencyOf(string targetWord, string textToView)
        {
             string[] textArray = textToView.Split(' ');
             int result = 0;
             foreach (string segment in textArray)
             {
                 if (segment.ToLower().Equals(targetWord.ToLower()))
                 {
                     result++;
                 }
             }
            return result;
        }
        /// <summary>
        /// Process by which a folder is loaded in, this uses the currFileList and currFile both to indicate the current position and the available options. 
        /// Will determinte also set the current mode to batch load meaning they are just text files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void batchLoad(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog myDialog = new FolderBrowserDialog();
            if (myDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = myDialog.SelectedPath;
                setMode("Batch Evaluation");
                testModeIsEnabled = false;
                string[] fileArray = Directory.EnumerateFiles(folderPath, "*.txt").ToArray();
                //Catches if files aren't present of the selected type
                if( fileArray.Length == 0)
                {
                    System.Windows.Forms.MessageBox.Show("I'm sorry the directoy you've selected does not have any files to load.");
                    return;
                }
                if (currFileList.Capacity != 0)
                {
                    currFileList.Clear();
                    currFileList = new List<string>(0);
                }
                
                foreach (string file in fileArray)
                {
                    currFileList.Add(file);
                }
                filePosition.Text = "1 out of " + (currFileList.Capacity-1).ToString();
                currfile = currFileList[0];
                string croppedFile = currfile.Substring(currfile.LastIndexOf("\\")+1);
                CurrFileName.Text = croppedFile;
                string fileContents = System.IO.File.ReadAllText(currFileList[0]);
                //Trims the current contents to remove any previous evaluation from the text
                if (fileContents.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileContents.IndexOf("-:Tyson Results:-");
                    fileContents = fileContents.Substring(0, endPoint);
                }
                Content.Text = fileContents; 
            }

        }
        /// <summary>
        /// Process by which a folder is loaded in, this uses the currFileList and currFile both to indicate the current position and the available options. 
        /// Will determinte also set the current mode to batch load meaning they are just text files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>       
        private void previousFile(object sender, RoutedEventArgs e)
        {
            if (currFileList.Count == 0 || currfile == null || currFileList.IndexOf(currfile) <= 0)
            {} 
            else
            {
                /*Variables
                 * Index: Where the desired file falls inside of the currFile List
                 *  currfile: Updated file location
                 *  croppedfile: The name of the file which is cleaned for display
                 *  fileContents: Raw unmanipulated file contents                 *  
                 */
                
                int index = currFileList.IndexOf(currfile) - 1 ;
                currfile = currFileList[index];
                string croppedFile = currfile.Substring(currfile.LastIndexOf("\\") + 1);
                CurrFileName.Text = croppedFile;
                string fileContents;
                //Logic whether or not the file is encrypted or not
                if (testModeIsEnabled == true)
                {
                    string orgText = System.IO.File.ReadAllText(currfile);
                    fileContents = decryptText(orgText);
                }
                else {
                    fileContents = System.IO.File.ReadAllText(currfile);
                }
                //Checks if the file has been evaluated if so removes that evaluation text for clearer display
                if (fileContents.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileContents.IndexOf("-:Tyson Results:-");
                    fileContents = fileContents.Substring(0, endPoint);
                }
                //Updates GUI values
                filePosition.Text =  (index+1).ToString() + " out of " + (currFileList.Count).ToString();
                Content.Text = fileContents;
                Update();
            }

        }
        /// <summary>
        /// Used when you want to move the currFile back within the code but not off the click listener
        /// </summary>
        private void previousFile()
        {
            if (currFileList.Count == 0 || currfile == null || currFileList.IndexOf(currfile) <= 0)
            {}
            else
            {

                /*Variables
                 * Index: Where the desired file falls inside of the currFile List
                 *  currfile: Updated file location
                 *  croppedfile: The name of the file which is cleaned for display
                 *  fileContents: Raw unmanipulated file contents                 *  
                 */

                int index = currFileList.IndexOf(currfile) - 1;
                currfile = currFileList[index];
                string croppedFile = currfile.Substring(currfile.LastIndexOf("\\") + 1);
                string fileContents;
                //Logic whether or not the file is encrypted or not
                if (testModeIsEnabled == true)
                {
                    string orgText = System.IO.File.ReadAllText(currfile);
                    fileContents = decryptText(orgText);
                }
                else
                {

                    fileContents = System.IO.File.ReadAllText(currfile);
                }
                //Checks if the file has been evaluated if so removes that evaluation text for clearer display
                if (fileContents.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileContents.IndexOf("-:Tyson Results:-");
                    fileContents = fileContents.Substring(0, endPoint);
                }

                //Updates GUI values
                Action act = () => { filePosition.Text = (index + 1).ToString() + " out of " + (currFileList.Count).ToString(); CurrFileName.Text = croppedFile; Content.Text = fileContents; };
                Content.Dispatcher.Invoke(act);
                Update();
            }

        }
        /// <summary>
        /// Used when you want to move the currFile back within the code but not off the click listener
        /// </summary>
        private void nextFile()
        {
            if (currFileList.Count == 0 || currfile == null || currFileList.IndexOf(currfile) + 1 > currFileList.Count - 1)
            {}

            else
            {

                /*Variables
                 * Index: Where the desired file falls inside of the currFile List
                 *  currfile: Updated file location
                 *  croppedfile: The name of the file which is cleaned for display
                 *  fileContents: Raw unmanipulated file contents                 *  
                 */
                int index = currFileList.IndexOf(currfile) + 1;
                currfile = currFileList[index];
                string croppedFile = currfile.Substring(currfile.LastIndexOf("\\") + 1);
                string fileContents;

                //Logic whether or not the file is encrypted or not
                if (testModeIsEnabled == true)
                {
                    string orgText = System.IO.File.ReadAllText(currfile);
                    fileContents = decryptText(orgText);
                }
                else
                {

                    fileContents = System.IO.File.ReadAllText(currfile);
                }
                //Checks if the file has been evaluated if so removes that evaluation text for clearer display
                if (fileContents.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileContents.IndexOf("-:Tyson Results:-");
                    fileContents = fileContents.Substring(0, endPoint);
                }
                //Updates GUI values
                Action act = () => { filePosition.Text = (index + 1).ToString() + " out of " + (currFileList.Count).ToString();  CurrFileName.Text = croppedFile; Content.Text = fileContents; };
                Content.Dispatcher.Invoke(act);
                Update();
            }
        }
        /// <summary>
        /// Process by which a folder is loaded in, this uses the currFileList and currFile both to indicate the current position and the available options. 
        /// Will determinte also set the current mode to batch load meaning they are just text files
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void nextFile(object sender, RoutedEventArgs e)
        {
            if (currFileList.Count == 0 || currfile == null || currFileList.IndexOf(currfile) + 1 > currFileList.Count - 1)
            {}          
            
            else
            {
                /*Variables
                 * Index: Where the desired file falls inside of the currFile List
                 *  currfile: Updated file location
                 *  croppedfile: The name of the file which is cleaned for display
                 *  fileContents: Raw unmanipulated file contents                 *  
                 */
                int index = currFileList.IndexOf(currfile) + 1;
                currfile = currFileList[index];
                string croppedFile = currfile.Substring(currfile.LastIndexOf("\\") + 1);
                CurrFileName.Text = croppedFile;
                string fileContents;
                //Logic whether or not the file is encrypted or not
                if (testModeIsEnabled == true)
                {
                    string orgText = System.IO.File.ReadAllText(currfile);
                    fileContents = decryptText(orgText);
                }
                else
                {

                    fileContents = System.IO.File.ReadAllText(currfile);
                }
                //Checks if the file has been evaluated if so removes that evaluation text for clearer display
                if (fileContents.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileContents.IndexOf("-:Tyson Results:-");
                    fileContents = fileContents.Substring(0, endPoint);
                }
                //Updates GUI values
                filePosition.Text = (index+1).ToString() + " out of " + (currFileList.Count).ToString();
                Content.Text = fileContents;
                Update();
            }
        }        
        //Needs to be finished
        private void completeCurrentBatch(object sender, RoutedEventArgs e)
        {
            while (currFileList.IndexOf(currfile) != 0)
            {
                previousFile();
            }

            foreach (string file in currFileList)
            {
                if (currentMode.Equals("Batch Test Evaluation"))
                {
                    string path = Directory.GetCurrentDirectory();
                    string fileName = "Result" + file.Substring(file.LastIndexOf("\\") + 1).Replace("tyTxt", ".txt");
                    using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(path+fileName, false))
                    {
                       
                        string Essay = Content.Text;
                        wordCount(Essay);
                        getSentences(Essay,testWordsMin);
                        outputFile.WriteLine(Essay);
                        outputFile.WriteLine("-:Tyson Results:-");
                        outputFile.WriteLine("----------------------------------------------------");
                        outputFile.WriteLine("Total Sentences: " + currentSentCount.ToString());
                        outputFile.WriteLine("Word Count: " + currentWordCount.ToString());
                        outputFile.WriteLine("Unique Words: " + (UniqueWords(Essay).ToString()));
                        outputFile.WriteLine("Test Requirements ");
                        outputFile.WriteLine("Minimum Words: " + testWordsMin.ToString());
                        outputFile.WriteLine("Minimum Sentences: " + testSentencesMin.ToString());
                        outputFile.WriteLine("Word Bank Provided");
                        foreach(string word in wordBank)
                        {
                            outputFile.WriteLine(word);
                        }

                        if (currentSentCount >= testSentencesMin && wordBankEval() == true)
                        {

                            outputFile.WriteLine("Test Criteria Met: Yes");

                        }
                        else {
                            outputFile.WriteLine("Test Criteria Met: No");
                        
                        
                        }
                        outputFile.WriteLine("File up to date as of: " + DateTime.Now.ToShortDateString());


                    }



                    nextFile();
                }
                else
                {
                    using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(file, false))
                    {
                        
                        string Essay = Content.Text;
                        wordCount(Essay);
                        getSentences(Essay);
                        outputFile.WriteLine(Essay);
                        outputFile.WriteLine("-:Tyson Results:-");
                        outputFile.WriteLine("----------------------------------------------------");
                        outputFile.WriteLine("Total Sentences: " + currentSentCount.ToString());
                        outputFile.WriteLine("Word Count: " + currentWordCount.ToString());
                        outputFile.WriteLine("Unique Words: " + (UniqueWords(Essay).ToString()));
                     
                        outputFile.WriteLine("File up to date as of: " + DateTime.Now.ToShortDateString());

                        nextFile();
                    }
                }
                
            }
        }

        //private void checkCriteria() {
            
        //    if (minSentences.Text.Equals("") && currentSentCount > 0)
        //    {
        //        paperPass = true;
        //        criteriaMet.IsChecked = true;
        //    }
        //    else if(currentSentCount==0 && minSentences.Text.Equals(""))
        //    {
        //        paperPass = false;
        //        criteriaMet.IsChecked = false;
        //        return;
        //    }
        //    else if (currentSentCount >= Convert.ToInt32(minSentences.Text))
        //    {
        //        paperPass = true;
        //        criteriaMet.IsChecked = true;
        //    }
        //    else {
        //        paperPass = false;
        //        criteriaMet.IsChecked = false;
        //    }
        
        
        //}
        /// <summary>
        /// Saves only the currently selected file and evaluates it.Provides
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exporttoFile(object sender, RoutedEventArgs e)
        {
            using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(currfile, false))
                {
                    string Essay = Content.Text;
                    outputFile.WriteLine(Essay);
                    outputFile.WriteLine("-:Tyson Results:-");
                    outputFile.WriteLine("----------------------------------------------------");
                    outputFile.WriteLine("Total Sentences: " + currentSentCount.ToString());
                    outputFile.WriteLine("Word Count: " + currentWordCount.ToString());
                    outputFile.WriteLine("Unique Words: " + (UniqueWords(Essay).ToString()));
                    
                    outputFile.WriteLine("File up to date as of: " + DateTime.Now.ToShortDateString());
                  }
        }
        


        /// <summary>
        /// Adds a single word to the current tests word bank and shows it via the listbox
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void submitWord(object sender, RoutedEventArgs e)
        {
                testWordBank.Add(testWordEntry.Text);
                updateListBoxTest();
        }
        /// <summary>
        /// Checks the current content against the current test word bank, not the word banks loaded into the page
        /// </summary>
        /// <returns></returns>
        private Boolean wordBankEval()
        {
            if (Content.Text.Length > 0)
            {
                List<string> words = wordBank;
                string content = Content.Text;


                foreach (string word in words)
                {
                    if (!content.ToLower().Contains(word.ToLower()))
                    {
                        return false;

                    }
                    Content.Select(content.IndexOf(word), word.Length);
                }
                if (words.Count > 0)
                {
                    return true;
                }
                else
                    return false;
            }
            else
                return false;
        }
        
        
        private void updateListBoxTest()
        {
            testWordBox.ItemsSource = testWordBank;
            testWordBox.Items.Refresh();
        }
        
        /// <summary>
        /// Removes a single word from the test word bank 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void removeSelectedTest(object sender, RoutedEventArgs e)
        {
           

            if (testWordBox.SelectedItem != null)
            {
                string selectedItem = testWordBox.SelectedItem.ToString();
                testWordBank.Remove(selectedItem);
                updateListBoxTest();

            }
        }
        
        private void exportTest(object sender, RoutedEventArgs e)
        {
            List<string> filecontents = new List<string>(0);
            /*Order of test settings 
             * Minimum Sentences
             * Minimum Words
             * Duration of Test
             * WordBank
            */
            List<bool> contents = new List<bool>(4);
            for(int i = 0; i < 4; i++)
            {
                contents.Add(false);
            }
            
            if(!minSentTest.Text.Equals("")){
                filecontents.Add( minSentTest.Text);
                contents.RemoveAt(0);
                contents.Insert(0, true);
            }
            if (!minWordsTest.Text.Equals("")) {
                filecontents.Add(minWordsTest.Text);
                contents.RemoveAt(1);
                contents.Insert(1, true);
            }
            if (!testDuration.Text.Equals(""))
            {
                filecontents.Add(testDuration.Text);
                contents.RemoveAt(2);
                contents.Insert(2, true);
            }
            
            if (testWordBank.Count>0)
            {
                foreach (string word in testWordBank)
                { 
                    filecontents.Add(word);                
                }
                contents.RemoveAt(3);
                contents.Insert(3, true);
            }
            //string input = Microsoft.VisualBasic.Interaction.InputBox("Please enter a an encryption key for this test, anything will work within 8 characters.", "Title", "Apple", -1, -1);
            //string testName = Microsoft.VisualBasic.Interaction.InputBox("What would you like to name this test?", "Test Name", "EnglishExamination1", -1, -1);
            //Microsoft.Win32.SaveFileDialog saver = new Microsoft.Win32.SaveFileDialog();
            //Nullable<bool> result = saver.ShowDialog();
            //if (result == true)
            //{
            
                string path = Directory.GetCurrentDirectory();
                System.Windows.Forms.MessageBox.Show(path);
                using (System.IO.StreamWriter file = new System.IO.StreamWriter(path+testName.Text+ ".tySon"))
                {
                    foreach (bool content in contents)
                    {
                        
                            file.WriteLine(content.ToString());
                        
                    }
                    foreach (string piece in filecontents)
                    {

                        file.WriteLine(piece);
                    }
                }
                
            

        }
        private void loadTest(object sender, RoutedEventArgs e)
        {
            testLoaded = true;
            List<bool> contents = new List<bool>(0);
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.Filter = "Tyson Test Files (.tySon)|*.tySon|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            bool? userClickedOK = openFileDialog1.ShowDialog();
            if (userClickedOK == true)
            {
                // Open the selected file to read.

                /*Order of test settings 
                * Minimum Sentences
                * Minimum Words
                * Duration of Test
                * WordBank
                */

                
                int currPointer = 4;
                string[] lines = System.IO.File.ReadAllLines(openFileDialog1.FileName);
                for (int i = 0; i < 4; i++)
                {
                    if (lines[i].ToLower().Equals("true"))
                    {
                        contents.Add(true);
                    }
                    else
                        contents.Add(false);
                
                }
                bool[] contentA = contents.ToArray();
                
                if (contentA[0] == true)
                {
                    testSentencesMin = Int32.Parse(lines[currPointer]);
                    //minSentences.Text = lines[currPointer];
                    currPointer++;
                    
                }
                if (contentA[1] == true)
                {
                    testWordsMin = Int32.Parse(lines[currPointer]);
                    //minWords.Text = lines[currPointer];
                    currPointer++;}
                if (contentA[2] == true)
                {
                    //timeOfTest.Text = lines[currPointer];
                        currPointer++;
                
                }
                if (contentA[3] == true)
                {
                    List<string> temporaryWordBank = new List<string>(0);
                    for (int index = currPointer; index < lines.Length; index++)
                    {
                        
                        temporaryWordBank.Add(lines[index]);
                    }
                    wordBank = temporaryWordBank;
                }
                
                
                Update();

            }
        }
        private void Window_Deactivated(object sender, EventArgs e)
        {
            if(testModeIsEnabled == true)
            {
            System.Environment.Exit(0);
            }
        }

        private void shutDown(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }

        //private void switchTestMode(object sender, RoutedEventArgs e)
        //{
        //    testModeIsEnabled = !testModeIsEnabled;
        //    if (testModeIsEnabled == true)
        //    {
        //        this.WindowStyle = WindowStyle.None;
        //        this.WindowState = WindowState.Maximized;
        //        testMode.IsChecked = true;
        //    }
        //    else
        //    {
        //        testMode.IsChecked = false;
        //        this.WindowStyle = WindowStyle.SingleBorderWindow;
        //    }
        //}

        private void encryptExport(object sender, RoutedEventArgs e)
        {

            byte[] keyArray;
            byte[] toEncryptArray = UTF8Encoding.UTF8.GetBytes(Content.Text);

            string key = "Marmalade";
            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            hashmd5.Clear();
            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();

            tdes.Key = keyArray;
            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;
            ICryptoTransform cTransform = tdes.CreateEncryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(toEncryptArray, 0,
             toEncryptArray.Length);
            tdes.Clear();
            string results = Convert.ToBase64String(resultArray, 0, resultArray.Length);

            if (currfile == null)
            {
                string outfileLocation = Microsoft.VisualBasic.Interaction.InputBox("Please provide a fileName to export to. (No extenstions just a name please)", "Title", "ExampleName", -1, -1);
                outfileLocation = outfileLocation + ".tySon";
                using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(outfileLocation, false))
                {
                    outputFile.WriteLine(results);
                }


            }
            else
            {
                currfile = currfile.Replace(".txt", ".tySon");
                System.Windows.Forms.MessageBox.Show(currfile);
                using (System.IO.StreamWriter outputFile = new System.IO.StreamWriter(currfile, false))
                {
                    outputFile.WriteLine(results);
                }
            }

            
        }

        private void decryptFile(object sender, RoutedEventArgs e)
        {
            String fileText = null;
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.Filter = "Tyson (.tySon)|*.tySon|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            bool? userClickedOK = openFileDialog1.ShowDialog();
            if (userClickedOK == true)
            {
                // Open the selected file to read.

                CurrFileName.Text = openFileDialog1.SafeFileName;

                fileText = System.IO.File.ReadAllText(openFileDialog1.FileName);
                currfile = openFileDialog1.FileName;
                filePosition.Text = "N/A";
                

            }

            byte[] keyArray;
            //get the byte code of the string
            if (fileText == null)
            {
                return;
            }
            byte[] toEncryptArray = Convert.FromBase64String(fileText);


            //Get your key from config file to open the lock!
            string key = "Marmalade";


            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;

            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(
                                 toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            Content.Text = UTF8Encoding.UTF8.GetString(resultArray);
            Update();
            


        }

        private string decryptText(String encryptedText)
        {

            if (encryptedText == " ")
            {
                return "";
            
            }

            byte[] keyArray;
            //get the byte code of the string

            byte[] toEncryptArray = Convert.FromBase64String(encryptedText);


            //Get your key from config file to open the lock!
            string key = "Marmalade";


            MD5CryptoServiceProvider hashmd5 = new MD5CryptoServiceProvider();
            keyArray = hashmd5.ComputeHash(UTF8Encoding.UTF8.GetBytes(key));
            hashmd5.Clear();

            TripleDESCryptoServiceProvider tdes = new TripleDESCryptoServiceProvider();
            tdes.Key = keyArray;

            tdes.Mode = CipherMode.ECB;
            tdes.Padding = PaddingMode.PKCS7;

            ICryptoTransform cTransform = tdes.CreateDecryptor();
            byte[] resultArray = cTransform.TransformFinalBlock(
                                 toEncryptArray, 0, toEncryptArray.Length);
            tdes.Clear();
            return UTF8Encoding.UTF8.GetString(resultArray);
            
        
        
        }

        private void switchTestForm(object sender, RoutedEventArgs e)
        {
            textEntryGrid.Visibility = System.Windows.Visibility.Collapsed;
            testForms.Visibility = System.Windows.Visibility.Visible;
        }

        private void switchEval(object sender, RoutedEventArgs e)
        {
            textEntryGrid.Visibility = System.Windows.Visibility.Visible;
            testForms.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void loadBank(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog1 = new Microsoft.Win32.OpenFileDialog();
            openFileDialog1.Filter = "Text Files (.txt)|*.txt|All Files (*.*)|*.*";
            openFileDialog1.FilterIndex = 1;
            openFileDialog1.Multiselect = false;
            bool? userClickedOK = openFileDialog1.ShowDialog();
            if (userClickedOK == true)
            {
                // Open the selected file to read.
                wordBankSection.Visibility = System.Windows.Visibility.Visible;
                temporaryBank.Clear();
                nameLoadedBank.Content = openFileDialog1.SafeFileName;
                
                String fileText = System.IO.File.ReadAllText(openFileDialog1.FileName);
                List<string> words = fileText.Split(',').ToList();
                foreach (string word in words)
                {

                   
                    temporaryBank.Add(word.ToLower());
                }

        
          }
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (testModeIsEnabled == true)
            {
                System.Windows.Application.Current.Shutdown();          
            
            }
            
        }

        private void testBatchLoad(object sender, RoutedEventArgs e)
        {
            if (testLoaded != true)
            {
                System.Windows.Forms.MessageBox.Show("I apologize you need to load in a test first to batch load tests forms. ");
                return;
            
            }
            FolderBrowserDialog myDialog = new FolderBrowserDialog();
            if (myDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string folderPath = myDialog.SelectedPath;
                setMode("Batch Test Evaluation");

                testModeIsEnabled = true;
                string[] fileArray = Directory.EnumerateFiles(folderPath, "*.tyTxt").ToArray();

                if( fileArray.Length == 0)
                {
                    System.Windows.Forms.MessageBox.Show("I'm sorry the directoy you've selected does not have any files to load.");
                    return;
                }
                if (currFileList.Capacity != 0)
                {
                    currFileList.Clear();
                    currFileList = new List<string>(0);
                
                }
                
                foreach (string file in fileArray)
                {
                    
                        currFileList.Add(file);
                    

                    //fileStream = new FileStream(file, FileMode.Open);
                    //reader = new StreamReader(fileStream);
                    
                    //fileStream.Close();
                }
                filePosition.Text = "1 out of " + (currFileList.Capacity-1).ToString();
                currfile = currFileList[0];
                
                string croppedFile = currfile.Substring(currfile.LastIndexOf("\\")+1);
                CurrFileName.Text = croppedFile;
                string currentText = System.IO.File.ReadAllText(currFileList[0]);
                string fileContents = decryptText(currentText);
                
                if (fileContents.Contains("-:Tyson Results:-"))
                {
                    int endPoint = fileContents.IndexOf("-:Tyson Results:-");
                    fileContents = fileContents.Substring(0, endPoint);



                }
                Content.Text = fileContents; 
            }

        
        }

        

        

       
      
       
      

        
        
    }
}
