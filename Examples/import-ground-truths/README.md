# Import Ground Truths

This example shows how you can generate json ground-truths files from an Excel source into an Azure Storage Container. You can deploy the `.env` file in the same directory where you are executing the code. Be sure to update anywhere where there is XXX to your specific Azure resources.

The Excel file is a template. You don't have to follow its format because you can configure the column mappings. However, if you have chat history, you will need to have a `GroupIdColumn` mapping. Be sure to use a row for each chat entry in a sequential manner.
