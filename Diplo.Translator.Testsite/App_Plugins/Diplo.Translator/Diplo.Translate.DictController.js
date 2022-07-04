(function () {
    'use strict';

    angular.module("umbraco")
        .controller("Diplo.Translate.DictController",
            function (diploTranslateResources, notificationsService, diploTranslatorHub) {
                var vm = this;
                vm.isLoading = false;
                vm.alert = null;
                vm.active = false;
                vm.overwrite = false;
                vm.languages = [];
                vm.langFrom = null;


                // Populates language select

                diploTranslateResources.getLanguages().then(function (data) {
                    vm.languages = data;
                    vm.langFrom = vm.languages.filter(lang => lang.IsDefault)[0];
                });

                // Checks the config is OK

                diploTranslateResources.checkConfiguration().then(function (response) {
                    if (!response.Ok) {
                        vm.alert = { alertType: "error", message: response.Message };
                        vm.active = false;
                        return;
                    }
                    else {
                        Init();
                    }
                });

                // Run after config is checked

                const Init = function () {
                    vm.active = true;
                    InitHub();
                }

                // Handles translate button click

                vm.translate = function () {

                    if (vm.overwrite) {
                        if (!window.confirm("You have chosen to overwrite existing dictionary values with new translations. Are you sure?")) {
                            return;
                        }
                    }

                    vm.buttonState = "busy";
                    vm.isLoading = true;
                    const clientId = getClientId();

                    // calls API service

                    diploTranslateResources.translateAllDictionary(clientId, vm.langFrom.IsoCode, vm.overwrite).then(function (response) {

                        vm.isLoading = false;
                        vm.buttonState = "success";

                        if (response.ErrorCount > 0) {
                            notificationsService.warning(response.Message);
                        }
                        else { // OK
                            notificationsService.success(response.Message);

                            // reload

                            setTimeout(function () {
                                window.location.reload(true);
                            }, 2000);
                        }
                    });
                }

                vm.toggle = function () {
                    vm.overwrite = !vm.overwrite;
                }

                // SignalR stuff

                function InitHub() {
                    diploTranslatorHub.initHub(function (hub) {
                        vm.hub = hub;

                        vm.hub.on('alert', function (data) {
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