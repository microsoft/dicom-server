/**
* This stored procedure provides the following functionality:
* - Completely deletes a collection of documents to delete (has a document link and ETag)
*
* @constructor
* @param {DeleteItem} items - The collection of documents to delete.
*/

function commit(items) {
    const response = getContext().getResponse();
    var collection = getContext().getCollection();

    let deletedResourceIdList = new Array();
    tryDelete(items);

    function tryDelete(documents) {
        if (documents.length > 0) {
            deletedResourceIdList.push(documents[0].documentLink);

            // Delete the first item.
            var isAccepted = collection.deleteDocument(
                documents[0].documentLink,
                { etag: documents[0].documentETag },
                function (err, resource) {
                    if (err) {
                        throw err;
                    }

                    // Successfully deleted the item, continue deleting.
                    documents.shift();
                    tryDelete(documents);
                });

            if (!isAccepted) {
                // We ran out of time.
                throwTooManyRequestsError();
            }
        } else {
            response.setBody(deletedResourceIdList);
        }
    }

    function throwTooManyRequestsError() {
        throw new Error(ErrorCodes.RequestEntityTooLarge, `The request could not be completed.`);
    }
}
