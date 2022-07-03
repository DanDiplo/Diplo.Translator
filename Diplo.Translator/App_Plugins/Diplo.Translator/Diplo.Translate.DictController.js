(function () {
    'use strict';

    angular.module("umbraco")
        .controller("Diplo.Translate.DictController",
            function (diploTranslateResources, notificationsService, diploTranslatorHub) {
                var vm = this;
                vm.isLoading = false;
                vm.alert = null;
                vm.active = false;

                diploTranslateResources.checkConfiguration().then(function (response) {

                    if (!response.Ok) {
                        vm.alert = { alertType: "error", message: response.Message };
                        vm.active = false;
                        return;
                    }
                    else {
                        vm.active = true;
                    }
                });

                InitHub();

                vm.translate = function () {

                    vm.buttonState = "busy";
                    vm.isLoading = true;

                    const clientId = getClientId();

                    diploTranslateResources.translateAllDictionary(clientId).then(function (response) {

                        console.log(response);

                        vm.isLoading = false;
                        vm.buttonState = "success";

                        if (response.ErrorCount > 0) {
                            notificationsService.warning(response.Message);
                        }
                        else {
                            notificationsService.success(response.Message);

                            setTimeout(function () {
                                window.location.reload(true);
                            }, 2000);
                        }
                    });
                }

                ////// SignalR things 
                function InitHub() {
                    diploTranslatorHub.initHub(function (hub) {
                        vm.hub = hub;

                        vm.hub.on('alert', function (data) {
                            console.log("Alert:", data);
                            vm.alert = data;
                        });

                        vm.hub.start();
                    });
                }

                function getClientId() {
                    if ($.connection !== undefined) {
                        return $.connection.connectionId;
                    }
                    return "";
                }

            });
})();