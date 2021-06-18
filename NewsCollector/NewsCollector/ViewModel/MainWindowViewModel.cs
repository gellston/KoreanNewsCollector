using DevExpress.Xpf.CodeView;
using DevExpress.Export;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using NewsCollector.Model;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

using DevExpress.Xpf.Grid;
using DevExpress.Xpf.Printing;
using DevExpress.XtraPrinting;

namespace NewsCollector.ViewModel
{
    public class MainWindowViewModel : ViewModelBase
    {

        public MainWindowViewModel()
        {

        }


        private string _Keyword = "";
        public string Keyword
        {
            get => _Keyword;
            set => Set(ref _Keyword, value);
        }


        private string _RelatedKeyword = "";
        public string RelatedKeyword
        {
            get => _RelatedKeyword;
            set => Set(ref _RelatedKeyword, value);
        }

        private string _SelectedRelatedKeyword = "";
        public string SelectedRelatedKeyword
        {
            get => _SelectedRelatedKeyword;
            set => Set(ref _SelectedRelatedKeyword, value);
        }

        private SearchResult _SelectedResult = null;
        public SearchResult SelectedResult
        {
            get => _SelectedResult;
            set => Set(ref _SelectedResult, value);
        }

        private ObservableCollection<string> _RelatedKeywordCollection = null;
        public ObservableCollection<string> RelatedKeywordCollection
        {
            get
            {
                _RelatedKeywordCollection ??= new ObservableCollection<string>();
                return _RelatedKeywordCollection;
            }
        }

        private ObservableCollection<SearchResult> _SearchResultCollection = null;
        public ObservableCollection<SearchResult> SearchResultCollection
        {
            get
            {
                _SearchResultCollection ??= new ObservableCollection<SearchResult>();
                return _SearchResultCollection;
            }
        }

        private bool _IsAnalyzing = false;
        public bool IsAnalyzing
        {
            get => _IsAnalyzing;
            set => Set(ref _IsAnalyzing, value);
        }



        public ICommand StartAnalysisCommand
        {
            get => new RelayCommand(() =>
            {

                if (this.Keyword.Length == 0) return;

                if (IsAnalyzing == true) return;
                IsAnalyzing = true;



                if (this.Keyword.Contains(" "))
                {
                    Helper.DialogHelper.ShowToastErrorMessage("키워드 오류", "공백 문자가 포함되어있습니다.");
                    this.IsAnalyzing = false;
                    return;
                }

                string str = @"[~!@\#$%^&*\()\=+|\\/:;?""<>']";
                System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(str);

                if (rex.IsMatch(this.Keyword))
                {
                    Helper.DialogHelper.ShowToastErrorMessage("키워드 오류", "검색 키워드에 특수문자가 섞여 있습니다.");
                    this.IsAnalyzing = false;
                    return;
                }




                this.SearchResultCollection.Clear();

                Task.Run(() =>
                {
                    ChromeDriverService service = ChromeDriverService.CreateDefaultService();
                    service.HideCommandPromptWindow = true;

                    var options = new ChromeOptions();
                    options.AddArgument("--window-position=-32000,-32000");
                    options.AddArgument("headless");


                    // THE PR News Link Searh
                    using (IWebDriver driver = new ChromeDriver(service, options))
                    {
                        var linkes = new List<string>();

                        try
                        {
                            for (int page = 1; page < 3; page++)
                            {
                                driver.Url = "http://www.the-pr.co.kr/news/articleList.html?page="+ page + "&total=486&sc_section_code=&sc_sub_section_code=&sc_serial_code=&sc_area=A&sc_level=&sc_article_type=&sc_view_level=&sc_sdate=&sc_edate=&sc_serial_number=&sc_word=" + this.Keyword;
                                // find 할때 찾을때까지 기다리는 seconds 설정
                                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);

                                // id="content" 라는 element에 있는 li tag 를 모두 가져옴
                                var linkeElements = driver.FindElements(By.ClassName("links"));

                                foreach (var link in linkeElements)
                                {
                                    string fullLink = link.GetAttribute("href");
                                    linkes.Add(fullLink);
                                }
                            }

                        }
                        catch(Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e.Message);
                        }


                        try
                        {
                            Console.WriteLine("link count " + linkes.Count);
                            int count = 0;
                            foreach (var link in linkes)
                            {

                                driver.Url = link;
                                driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(2);
                                var titleElement = driver.FindElements(By.ClassName("article-head-title"));
                                var title = titleElement[0].Text;



                                var contentElement = driver.FindElements(By.Id("article-view-content-div"));
                                var content = contentElement[0].Text;


                                var articleTimeElement = driver.FindElement(By.XPath("//meta[@property='article:published_time']"));
                                var articleTime = articleTimeElement.GetAttribute("content").Split("T")[0];


                                var categoryElement = driver.FindElement(By.XPath("//meta[@name='Classification']"));
                                var category = categoryElement.GetAttribute("content");


                                var newsNameElement = driver.FindElement(By.XPath("//meta[@property='og:site_name']"));
                                var newsName = newsNameElement.GetAttribute("content");

                                var result = new SearchResult()
                                {
                                    NewsName = newsName,
                                    Title = title,
                                    Link = link,
                                    Date = articleTime,
                                    Category = category,
                                    Content = content,
                                    PublishedDate = articleTime
                                    
                                };

                                this.CheckKeywordFrequency(this.RelatedKeywordCollection, result);

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    this.SearchResultCollection.Add(result);
                                    count++;

                                });


                            }

                        }
                        catch (Exception e)
                        {
                            System.Diagnostics.Debug.WriteLine(e.Message);
                        }



                        try
                        {
                            var collection =  new ObservableCollection<SearchResult>(SearchResultCollection.OrderByDescending(i => i.TotalFrequency));
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                this.SearchResultCollection.Clear();
                                this.SearchResultCollection.AddRange(collection);

                            });
                        }
                        catch(Exception e)
                        {

                        }
                        

                    }

                    this.IsAnalyzing = false;
                });

            });

        }

        private void CheckKeywordFrequency(ObservableCollection<string> keyword, SearchResult searchResult)
        {
            foreach (var key in keyword)
            {
                int count = 0;
                count = searchResult.Content.Split(key).Length;
                count += searchResult.Content.Split(key).Length;
                if (count > 0)
                {
                    searchResult.KeywordFrequency.Add(new RelatedKeywordCount()
                    {
                        Frequency = count,
                        Keyword = key

                    });
                }
            }

            foreach(var frequency in searchResult.KeywordFrequency)
            {
                searchResult.TotalFrequency += frequency.Frequency;
            }
        }

        public ICommand AddRelatedKeywordCommand
        {
            get => new RelayCommand(() =>
            {
                if (this.RelatedKeywordCollection.Contains(this.RelatedKeyword) == true) return;
                if (this.RelatedKeyword.Length == 0) return;

                string str = @"[~!@\#$%^&*\()\=+|\\/:;?""<>']";
                System.Text.RegularExpressions.Regex rex = new System.Text.RegularExpressions.Regex(str);

                if (rex.IsMatch(this.RelatedKeyword))
                {
                    Helper.DialogHelper.ShowToastErrorMessage("키워드 오류", "검색 키워드에 특수문자가 섞여 있습니다.");
                    return;
                }


                if (this.RelatedKeyword.Contains(" "))
                {
                    Helper.DialogHelper.ShowToastErrorMessage("키워드 오류", "공백 문자가 포함되어있습니다.");
                    return;
                }

                this.RelatedKeywordCollection.Add(this.RelatedKeyword);
            });
        }


        public ICommand ExportExcelCommand
        {
            get => new RelayCommand<TableView>((control) =>
            {
                try
                {
                    var filePath = Helper.DialogHelper.SaveFile("Script File (.xlsx)|*.xlsx");
                    XlsxExportOptionsEx xlsxOptions = new XlsxExportOptionsEx();
                    xlsxOptions.ShowGridLines = true;   // 라인출력 
                    xlsxOptions.SheetName = "분석 결과";    // sheet 명
                    xlsxOptions.ExportType = DevExpress.Export.ExportType.DataAware;    // ExportType

                    control.ExportToXlsx(filePath, xlsxOptions);
            
                }
                catch(Exception e)
                {
                    Helper.DialogHelper.ShowErrorMessage(e.Message);
                }




 
            });
        }

        public ICommand SaveSettingCommand
        {
            get => new RelayCommand(() =>
            {

            });
        }

        public ICommand DeleteRelatedKeywordCommand
        {
            get => new RelayCommand(() =>
            {
                if (this.SelectedRelatedKeyword != null)
                    this.RelatedKeywordCollection.Remove(this.SelectedRelatedKeyword);
            });
        }
    }
}
