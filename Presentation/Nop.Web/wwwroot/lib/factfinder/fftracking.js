
var tracking = {
    doTrack : function(eventName, sessionId, extraParams) { 
        var params = { 
            event : eventName, 
            channel: 'nopcom4_produkte', 
            sid : sessionId 
        };

        for (var param in extraParams) { 
            if (extraParams[param] != null) 
				params[param] = extraParams[param]; 
        } 
        $.ajax({ 
            type : "GET", 
            url : '/FactFinderSearch/TrackingProxy', 
            data : params, 
            contentType : "application/x-www-form-urlencoded; charset=UTF-8", 
            cache : false, 
            async: false,
            complete: function (xhr, textStatus) {
                console.log(xhr.status + ': ' + textStatus);
            }			
        })
        .done(function (data) { /* kann auskommentiert werden, wenn produktiv */ 
            console.log(data);
        });
    }, 
    click: function (sessionId, id, masterId, query, pos, origPos, page, pageSize, origPageSize, simi) { 
        this.doTrack('click', sessionId, { 
            id : id, 
            masterId : masterId, 
            query : query, 
            pos : pos, 
            origPos : origPos, 
            page: page, 
            pageSize: pageSize,
            origPageSize: origPageSize ,
            simi: simi
        }); 
    }, 
    recommendationClick : function(sessionId, id, masterId, mainId) { 
        this.doTrack('recommendationClick', sessionId, { 
            id : id, 
            masterId : masterId, 
            mainId : mainId 
        }); 
    },
    cart: function (sessionId, id, masterId, count, price, query) {
        this.doTrack('cart', sessionId, {
            id: id,
            masterId: masterId,
            count: count,
            price: price,
            query: query
        });
    },
    directCart: function (sessionId, id, masterId, query, pos, origPos, page, origPageSize, count, price) {
        this.click(sessionId, id, masterId, query, pos, origPos, page, origPageSize);
        this.cart(sessionId, id, masterId, count, price, query);
    },
    checkout: function (sessionId, id, masterId, count, price, query, userId) {
        this.doTrack('checkout', sessionId, {
            id: id,
            masterId: masterId,
            count: count,
            price: price,
            query: query,
            userId: userId
        });
    },
    login: function (sessionId, userId) {
        this.doTrack('login', sessionId, {
            userId: userId
        });
    }
}
