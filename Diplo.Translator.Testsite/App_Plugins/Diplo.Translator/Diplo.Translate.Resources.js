(function () {
    'use strict';

    const translateApiUrl = "/Umbraco/backoffice/DiploTranslator/TranslationApi/";

    angular.module('umbraco.resources').factory('diploTranslateResources', function ($http, umbRequestHelper) {
        return {
            translateAllDictionary: function (clientId, fromCulture, overwrite) {
                return umbRequestHelper.resourcePromise(
                    $http({
                        method: 'POST',
                        url: translateApiUrl + "TranslateAll?clientId=" + clientId + "&fromCulture=" + fromCulture + "&overwrite=" + overwrite
                    })
                );
            },
            checkConfiguration: function () {
                return umbRequestHelper.resourcePromise(
                    $http.get(translateApiUrl + "CheckConfiguration")
                );
            },
            getLanguages: function () {
                return umbRequestHelper.resourcePromise(
                    $http.get(translateApiUrl + "GetLanguages")
                );
            }
        };
    });
})();