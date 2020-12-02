using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace Duplify
{
    public class LvObject
    {
        //Строка без CheckBox
        public LvObject(string name, string value)
        {
            if (name.Length >= 50)
            {
                Name = name.Substring(0, 23) + "..." + name.Substring(name.Length - 24, 24);
            }
            else Name = name;
            Value = value;
            IsCheckBox = false;
            ItemBackgroundColor = true;
        }
        //Строка с CheckBox
        public LvObject(string name, double size)
        {
            if (name.Length >= 50)
            {
                Name = name.Substring(0, 23) + "..." + name.Substring(name.Length - 24, 24);
            }
            else Name = name;
            Value = name;
            IsCheckBox = true;
            if (size >= 1000)
            {
                Size = (Math.Round(size / 1024, 2)).ToString() + "МБ";
            }
            else if (size >= 1000000)
            {
                Size = (Math.Round(size / 1048756, 2)).ToString() + "ГБ";
            }
            else { Size = size.ToString() + "КБ"; }
            ItemBackgroundColor = false;
            CheckBoxIsChecked = false;
        }

        public string Name { get; private set; }
        public string Size { get; private set; }
        public string Value { get; private set; }
        public bool IsCheckBox { get; private set; }
        public bool CheckBoxIsChecked { get; set; }
        public bool ItemBackgroundColor { get; private set; }

    }


    //Выбор шаблона в третьем столбце (empty, CheckBox или error)
    public class ListViewItemsTemplateSelector : DataTemplateSelector
    {
        public DataTemplate CheckBoxTemplate { get; set; }
        public DataTemplate EmptyTemplate { get; set; }
        public DataTemplate ErrorTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            LvObject lv_object = item as LvObject;
            try
            {
                if (lv_object.IsCheckBox)
                {
                    return CheckBoxTemplate;
                }
                return EmptyTemplate;
            }
            catch (NullReferenceException)
            {
                return ErrorTemplate;
            }

        }

    }
}
