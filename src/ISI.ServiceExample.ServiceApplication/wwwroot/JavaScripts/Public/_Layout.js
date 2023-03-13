jQuery.namespace("ISI.ServiceExample.ServiceApplication.Public.Layout", function(jQuery) {
	var model = {};
	var view = {};
	var controller = {
		setup: function (config) {
			controller.eventBinder();
		},
		eventBinder: function () {
		}
	};

	return {
		Setup: controller.setup
	};
} (jQuery));

jQuery(document).ready(function (jQuery) {
	ISI.ServiceExample.ServiceApplication.Public.Layout.Setup();
});