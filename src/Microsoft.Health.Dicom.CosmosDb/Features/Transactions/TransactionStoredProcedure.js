function commit(itemsString) {
    var collection = getContext().getCollection();
    var items = JSON.parse(itemsString);

    items.forEach(function (item) {
        switch (item.Operation) {
            // Delete
            case 0:
                collection.deleteDocument(item.DocumentLink, { etag: item.DocumentETag }, commitCallback);
                break;
        }
    });
}

function commitCallback(error, resource) {
    if (error) {
        throw new Error(error);
    }

    getContext().getResponse().setBody(resource.id);
}
