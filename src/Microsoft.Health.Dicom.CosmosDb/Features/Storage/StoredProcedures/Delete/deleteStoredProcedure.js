/**
* This stored procedure provides the following functionality:
* - Completely deletes a collection of documents to delete (has a document link and ETag)
*
* @constructor
* @param {DeleteItem} items - The collection of documents to delete.
*/

function commit(items) {
    const response = getContext().getResponse();
    const collection = getContext().getCollection();

    let deletedResourceIdList = new Array();

    items.forEach(function (item) {
        deletedResourceIdList.push(item.documentLink);

        var isAccepted = collection.deleteDocument(
            item.documentLink,
            { etag: item.documentETag },
            function (err, resource) {
                if (err) {
                    throw err;
                }

                // Successfully deleted the item, continue deleting.
            });

        if (!isAccepted) {
            // We ran out of time.
            throw new Error(ErrorCodes.RequestEntityTooLarge, `The request could not be completed.`);
        }
    });

    response.setBody(deletedResourceIdList);
}
