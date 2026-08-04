using System;
using System.Collections.Generic;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using SeleniumExtras.WaitHelpers;
using TravelGuide.Models;

namespace TravelGuide.Runners;

// ReSharper disable once ConvertToPrimaryConstructor
public sealed class GeorgianTravelGuideRunner
{
    private readonly IWebDriver _driver;
    private readonly List<PlaceModel> _places = [];
    private readonly string _startPoint;

    public GeorgianTravelGuideRunner(IWebDriver driver, string startPoint)
    {
        _driver = driver;
        _startPoint = startPoint;
    }

    public bool Run()
    {
        _driver.Navigate().GoToUrl(_startPoint);
        var pageTitle = _driver.Title;
        Console.WriteLine("Page Title: " + pageTitle);

        _driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(60);
        _driver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(600);
        _driver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(600);

        if (!FindElementAndClick("რა ვნახოთ", "a", true))
        {
            return false;
        }

        if (!FindElementAndClick("ყველას ნახვა "))
        {
            return false;
        }

        FindElementAndWaitUntilDisappear("იტვირთება...", "span");

        if (!FindElementByClassNameAndClick("icon-arrow-down"))
        {
            return false;
        }

        if (!FindElementAndClick("მანძილი", "div"))
        {
            return false;
        }

        FindElementAndWaitUntilDisappear("იტვირთება...", "span");

        if (!FindElementByClassNameAndClick("remove"))
        {
            return false;
        }

        FindElementAndWaitUntilDisappear("იტვირთება...", "span");

        var lastCount = 0;
        var sameCount = 0;
        const int maxSameCount = 100;

        List<string> urlList = FindAllLinksByClassName();

        while (lastCount < urlList.Count || sameCount < maxSameCount)
        {
            if (lastCount == urlList.Count)
            {
                sameCount++;
                Console.WriteLine($"Same count: {sameCount}");
            }
            else
            {
                sameCount = 0;
            }

            lastCount = urlList.Count;
            Console.WriteLine($"lastCount: {lastCount}");

            for (var i = 0; i < 10; i++)
            {
                // Send Arrow Down to the body of the page
                _driver.FindElement(By.TagName("body")).SendKeys(Keys.ArrowDown);
            }

            //// Send Page Down to the body of the page
            //_driver.FindElement(By.TagName("body")).SendKeys(Keys.PageDown);

            //var js = (IJavaScriptExecutor)_driver;
            //js.ExecuteScript("window.scrollBy(0, window.innerHeight);");

            FindElementAndWaitUntilDisappear("სანახავი ადგილი", "span");

            urlList = FindAllLinksByClassName();
        }

        urlList.Except(_places.Select(s => s.Url)).ToList().ForEach(url =>
        {
            var place = new PlaceModel { Url = url };
            _places.Add(place);
        });

        //_driver.SwitchTo().NewWindow(WindowType.Tab);

        //foreach (var place in _places) WorkWithPlace(place);

        Console.WriteLine("Success");
        return true;
    }

    //private void WorkWithPlace(PlaceModel place)
    //{
    //    _driver.Navigate().GoToUrl(place.Url);

    //    _driver.SwitchTo().Window(_driver.WindowHandles.Last());

    //    WaitForPageLoad();

    //    var header = _driver.FindElement(By.TagName("h1"));
    //    place.HeaderText = header.Text;

    //    var contentDiv =
    //        _driver.FindElement(By.XPath("//span[@class='icon-location']/following-sibling::div[@class='content']"));
    //    place.Location = contentDiv.Text;

    //    Console.WriteLine($"{place.HeaderText} - {place.Location}");
    //}

    private List<string> FindAllLinksByClassName()
    {
        //IReadOnlyCollection<IWebElement> links = _driver.FindElements(By.XPath("//article[@class='g-card']//a[@class='picture']"));
        IReadOnlyCollection<IWebElement> links = _driver.FindElements(By.CssSelector("article.g-card a.picture"));

        var result = new List<string>();
        foreach (var link in links)
        {
            var href = link.GetAttribute("href");
            if (!string.IsNullOrEmpty(href))
            {
                result.Add(href);
            }
        }

        return result;
    }

    private bool FindElementByClassNameAndClick(string className)
    {
        try
        {
            WaitForPageLoad();

            var by = By.ClassName(className);
            var element = _driver.FindElement(by);

            WaitForElementToBeClickable(by);

            if (element is { Displayed: true, Enabled: true })
            {
                Console.WriteLine($"Clicking on element with class name: {className}");
                element.Click();
                return true;
            }

            Console.WriteLine($"Element with class name '{className}' is not interactable.");
            return false;
        }
        catch (NoSuchElementException)
        {
            Console.WriteLine($"Element with class name '{className}' not found.");
            return false;
        }
    }

    private void WaitForPageLoad()
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(driver =>
            Equals(((IJavaScriptExecutor)driver).ExecuteScript("return document.readyState"), "complete"));
    }

    private void WaitForElementToBeClickable(By by)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
        wait.Until(ExpectedConditions.ElementToBeClickable(by));
    }

    //private void WaitForElementToBeVisible(By by)
    //{
    //    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    //    wait.Until(ExpectedConditions.ElementIsVisible(by));
    //}

    //private void WaitForElementToBePresent(By by)
    //{
    //    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    //    wait.Until(ExpectedConditions.ElementExists(by));
    //}

    private void WaitForElementToNotDisplayed(By by)
    {
        var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(60));
        wait.Until(driver => !driver.FindElements(by).Any(e => e.Displayed));
    }

    //private void WaitForJQueryToFinish()
    //{
    //    var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
    //    wait.Until(driver =>
    //        (bool)((IJavaScriptExecutor)driver).ExecuteScript(
    //            "return window.jQuery != undefined && jQuery.active == 0"));
    //}

    private void FindElementAndWaitUntilDisappear(string captionToFind, string elementName = "a")
    {
        WaitForPageLoad();
        var by = By.XPath($"//{elementName}[text()='{captionToFind}']");
        WaitForElementToNotDisplayed(by);
    }

    private bool FindElementAndClick(string captionToFind, string elementName = "a", bool useNavigate = false)
    {
        WaitForPageLoad();

        var by = By.XPath($"//{elementName}[text()='{captionToFind}']");
        var element = _driver.FindElement(by);

        if (useNavigate)
        {
            var href = element.GetAttribute("href");
            if (string.IsNullOrEmpty(href))
            {
                Console.WriteLine($"The '{captionToFind}' link does not have a valid href attribute.");
                return false;
            }

            Console.WriteLine($"Navigating to: {href}");
            _driver.Navigate().GoToUrl(href);
            return true;
        }

        WaitForElementToBeClickable(by);

        Console.WriteLine($"Click on element: {element.Text}");
        if (element is { Displayed: true, Enabled: true })
        {
            element.Click();
        }
        else
        {
            Console.WriteLine("Element is not interactable.");
            return false;
        }

        return true;
    }
}
