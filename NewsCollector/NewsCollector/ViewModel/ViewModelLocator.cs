using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Ioc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace NewsCollector.ViewModel
{
    public class ViewModelLocator
    {
        public ViewModelLocator()
        {
            try
            {
                var processes = Process.GetProcessesByName("chromedriver");
                foreach(var process in processes)
                {
                    process.Kill();
                }
            }catch(Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e.Message);
            }
            SimpleIoc.Default.Register<MainWindowViewModel>();
        }


        ~ViewModelLocator()
        {
   
        }


        public ViewModelBase MainWindowViewModel
        {
            get => SimpleIoc.Default.GetInstance<MainWindowViewModel>();
        }
    }
}
