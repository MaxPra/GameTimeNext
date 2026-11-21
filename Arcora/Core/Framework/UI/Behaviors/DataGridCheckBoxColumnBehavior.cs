using System.Windows;
using System.Windows.Controls;

namespace GameTimeNext.Core.Framework.UI.Behaviors
{
    public static class DataGridCheckBoxColumnBehavior
    {
        public static readonly DependencyProperty UseCustomCheckBoxColumnStyleProperty = DependencyProperty.RegisterAttached(
            "UseCustomCheckBoxColumnStyle",
            typeof(bool),
            typeof(DataGridCheckBoxColumnBehavior),
            new PropertyMetadata(false, OnUseCustomCheckBoxColumnStyleChanged));

        public static bool GetUseCustomCheckBoxColumnStyle(DependencyObject obj)
        {
            return (bool)obj.GetValue(UseCustomCheckBoxColumnStyleProperty);
        }

        public static void SetUseCustomCheckBoxColumnStyle(DependencyObject obj, bool value)
        {
            obj.SetValue(UseCustomCheckBoxColumnStyleProperty, value);
        }

        private static void OnUseCustomCheckBoxColumnStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not DataGrid dataGrid)
                return;

            bool enabled = (bool)e.NewValue;

            if (enabled)
            {
                dataGrid.Loaded += DataGrid_Loaded;
                dataGrid.AutoGeneratingColumn += DataGrid_AutoGeneratingColumn;

                ApplyStyles(dataGrid);
                return;
            }

            dataGrid.Loaded -= DataGrid_Loaded;
            dataGrid.AutoGeneratingColumn -= DataGrid_AutoGeneratingColumn;
        }

        private static void DataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is DataGrid dataGrid)
                ApplyStyles(dataGrid);
        }

        private static void DataGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (sender is not DataGrid dataGrid || e.Column is not DataGridCheckBoxColumn column)
                return;

            ApplyStyleToColumn(dataGrid, column);
        }

        private static void ApplyStyles(DataGrid dataGrid)
        {
            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                if (dataGrid.Columns[i] is DataGridCheckBoxColumn column)
                    ApplyStyleToColumn(dataGrid, column);
            }
        }

        private static void ApplyStyleToColumn(DataGrid dataGrid, DataGridCheckBoxColumn column)
        {
            if (dataGrid.TryFindResource("DataGrid.CheckBox.Display") is Style displayStyle)
                column.ElementStyle = displayStyle;

            if (dataGrid.TryFindResource("DataGrid.CheckBox.Edit") is Style editingStyle)
                column.EditingElementStyle = editingStyle;
        }
    }
}
