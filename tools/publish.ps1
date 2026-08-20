cd "C:\Users\Dell\Desktop\Shopfreshers\shopfreshers-backend"
dotnet publish ShopFresherz.API -c Release -o "C:\Users\Dell\Desktop\Shopfreshers\shopfreshers-backend\publish" --no-restore 2>&1 | Select-Object -Last 30
