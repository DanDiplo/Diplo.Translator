(function () {
    'use strict';

    const translateApiUrl = "/Umbraco/backoffice/DiploTranslator/TranslationApi/";

    angular.module('umbraco.resources').factory('diploTranslateResources', function ($http, umbRequestHelper) {
        return {
            translateAllDictionary: function (clientId) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        method: 'POST',
                        url: translateApiUrl + "TranslateAll?clientId=" + clientId
                    })
                );
            }
        };
    });
})();