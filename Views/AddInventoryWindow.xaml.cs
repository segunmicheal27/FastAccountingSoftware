using FastAccountingSoftware.Models;
using System.Windows;
using System;

namespace FastAccountingSoftware.Views
{
    public partial class AddInventoryWindow : Window
    {
        private InventoryItem? _editingItem;
        public InventoryItem? NewItem { get; private set; }

        public AddInventoryWindow(InventoryItem? itemToEdit = null)
        {
            InitializeComponent();
            _editingItem = itemToEdit;

            if (_editingItem != null)
            {
                TitleText.Text = "Edit Product Details";
                NameInput.Text = _editingItem.Name;
                QuantityInput.Text = _editingItem.Quantity.ToString();
                CostPriceInput.Text = _editingItem.CostPrice.ToString("N0");
                SellingPriceInput.Text = _editingItem.SellingPrice.ToString("N0");
                ReorderLevelInput.Text = _editingItem.ReorderLevel.ToString();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(NameInput.Text))
            {
                CustomMessageBox.Show("Please enter a product name.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int.TryParse(QuantityInput.Text.Trim(), out int qty);
            
            string rawCost = CostPriceInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawCost, out double cost);

            string rawSelling = SellingPriceInput.Text.Replace(",", "").Replace("₦", "").Trim();
            double.TryParse(rawSelling, out double selling);

            int.TryParse(ReorderLevelInput.Text.Trim(), out int reorder);

            if (_editingItem != null)
            {
                _editingItem.Name = NameInput.Text.Trim();
                _editingItem.Quantity = qty;
                _editingItem.CostPrice = cost;
                _editingItem.SellingPrice = selling;
                _editingItem.ReorderLevel = reorder;
                NewItem = _editingItem;
            }
            else
            {
                NewItem = new InventoryItem
                {
                    Name = NameInput.Text.Trim(),
                    Quantity = qty,
                    CostPrice = cost,
                    SellingPrice = selling,
                    ReorderLevel = reorder
                };
            }

            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
