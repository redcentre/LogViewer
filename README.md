# Overview

The **LogViewer** project is a Windows&#x2122; desktop utility that searches and displays logging records that are stored in Azure Tables by [Red Centre Software][redc] applications. The project source code and MSI installer file are publicly available for anyone who might find them useful.

The logging table schemas (property/column names) differ slightly across different generations of tables, but this utility attempts to unify the view of their contents.

> NOTE - Although this utility was created to view specific types of logging tables, it can search and display the records in any Azure Table. However, for general purpose Azure Table data management, it would be better to use a cross-platform tool like [Azure Storage Explorer][azexplore].

----

## Credentials

**LogViewer** scans an Azure Subscription for Storage Accounts and Tables to build a navigation tree, it therefore requires powerful credentials which must be entered into its settings before it can become operational. The credentials are described in the following table.

| Credential | Source |
| ---------- | ------ |
|Subscription&#xa0;Id | A guid-like string taken from the Azure portal > Subscriptions > Account blade. Clearly labelled. |
| Tenant&#xa0;Id | A guid-like string taken from the Azure portal > AD blade. Clearly labelled. |
| Application&#xa0;Id | A guid-like string which is the Id of an application created in the Azure portal > AD > App registrations blade. The app must be given a role in the Azure portal > Subscriptions > IAM blade so it has read access to the susbcription. |
| Client&#xa0;secret | Created in the Azure portal > App registrations > Certificates & secrets blade.

The following screenshot shows where the four credentials are entered into the Settings dialog.

![Settings][imgsett]

----

## Main Window

The following screenshot shows the main window which is divided into three panels.

- Top-left is the navigation tree which list the hierarchy of storage accounts and any tables they contain. Loading the tree requires a complete scan of the subscription, which may be slow if many resources are defined. The tree is cached for 30 minutes to improve performance on repeated starts of the application. The **Find Tables** button will force a reload at any time.
- Bottom-left are the table search parameters which become enabled when a table is selected in the navigation tree. The parameters can be used to find logging records of interest by their key, date range and logging event Ids.
- To the right of the vertical splitter is a data grid containing the search results. Columns can be rearranged and teir data sorted. The *Quick filter* text box at the far bottom left of the window can be used to filter the data grid in-place.

![Settings][imgwin]

----

## Analyse

It is often difficult to know what date ranges limits and unique keys are stored in a table, especially if the table contains very large numbers of records. **LogViewer** provides a table analysis feature that scans all table records and reports the timestamp ranges and unique Partition Keys found in the table.

The following screenshot shows the results of a table analysis.

- The top panel list the minimim and maximum timestamps and Row Keys found in the whole table.
- The grid display up to 10000 unique Partition Keys found in the table and the minimum and maximum timestamps and Row Keys within that Partition Key.
- The bottom panel assists in the deletion of old records, which is a task often required when managing the size of logging tables.

![Settings][imganal]

05-May-2025

[redc]: https://www.redcentresoftware.com/
[azexplore]: https://azure.microsoft.com/en-us/products/storage/storage-explorer
[appid]: https://gfkeogh.blogspot.com/2021/04/enumerate-azure-storage-accounts.html
[imgsett]: https://orthoprog.blob.core.windows.net/reference/wiki-img/LogViewer-Settings.png
[imgwin]: https://orthoprog.blob.core.windows.net/reference/wiki-img/LogViewer-Window.png
[imganal]: https://orthoprog.blob.core.windows.net/reference/wiki-img/LogViewer-Analyse.png