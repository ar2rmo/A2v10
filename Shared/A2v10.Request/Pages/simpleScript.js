// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

(function () {

	$(Locale)

	const vm = new Vue({
		el: "#app",
		data: {
			info: $(PageData),
			appLinks: $(AppLinks),
			processing: false
		},
		computed: {
			locale() {
				return window.$$locale;
			}
		},
		methods: {
		}
	});
})();