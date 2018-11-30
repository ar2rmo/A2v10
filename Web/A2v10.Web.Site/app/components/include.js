﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

// 20180426-7167
/*components/include.js*/

(function () {

	const http = require('std:http');
	const urlTools = require('std:url');

	function _destroyElement(el) {
		let fc = el.firstElementChild;
		if (!fc) return;
		let vue = fc.__vue__;
		// Maybe collectionView created the wrapper!
		if (vue && !vue.$marker)
			vue = vue.$parent;
		if (vue && vue.$marker()) {
			vue.$destroy();
		}
	}

	Vue.component('include', {
		template: '<div :class="implClass"></div>',
		props: {
			src: String,
			cssClass: String,
			needReload: Boolean,
			done: Function
		},
		data() {
			return {
				loading: true,
				currentUrl: '',
				_needReload: true
			};
		},
		methods: {
			loaded(ok) {
				this.loading = false;
				if (this.done)
					this.done();
			},
			requery() {
				if (this.currentUrl) {
					// Do not set loading. Avoid blinking
					this.__destroy();
					http.load(this.currentUrl, this.$el).then(this.loaded);
				}
			},
			__destroy() {
				//console.warn('include has been destroyed');
				_destroyElement(this.$el);
			}
		},
		computed: {
			implClass() {
				return `include ${this.cssClass || ''} ${this.loading ? 'loading' : ''}`;
			}
		},
		mounted() {
			//console.warn('include has been mounted');
			if (this.src) {
				this.currentUrl = this.src;
				http.load(this.src, this.$el).then(this.loaded);
			}
		},
		destroyed() {
			this.__destroy(); // and for dialogs too
		},
		watch: {
			src: function (newUrl, oldUrl) {
				if (newUrl.split('?')[0] === oldUrl.split('?')[0]) {
					// Only the search has changed. No need to reload.
					this.currentUrl = newUrl;
				}
				else if (urlTools.idChangedOnly(newUrl, oldUrl)) {
					// Id has changed after save. No need to reload.
					this.currentUrl = newUrl;
				} else if (urlTools.idOrCopyChanged(newUrl, oldUrl)) {
					// Id has changed after save. No need to reload.
					this.currentUrl = newUrl;
				}
				else {
					this.loading = true; // hides the current view
					this.currentUrl = newUrl;
					this.__destroy();
					http.load(newUrl, this.$el).then(this.loaded);
				}
			},
			needReload(val) {
				// works like a trigger
				if (val) this.requery();
			}
		}
	});


	Vue.component('a2-include', {
		template: '<div class="a2-include"></div>',
		props: {
			source: String,
			arg: undefined
		},
		data() {
			return {
				needLoad: 0
			};
		},
		methods: {
			__destroy() {
				//console.warn('include has been destroyed');
				_destroyElement(this.$el);
			},
			loaded() {

			},
			makeUrl() {
				let arg = this.arg || '0';
				return urlTools.combine('_page', this.source, arg);
			},
			load() {
				let url = this.makeUrl();
				this.__destroy();
				http.load(url, this.$el).then(this.loaded);
			}
		},
		watch: {
			source(newVal, oldVal) {
				//console.warn(`source changed ${newVal}, ${oldVal}`);
				this.needLoad += 1;
			},
			arg(newVal, oldVal) {
				//console.warn(`arg changed ${newVal}, ${oldVal}`);
				this.needLoad += 1;
			},
			needLoad() {
				//console.warn(`load iteration: ${this.needLoad}`);
				this.load();
			}
		},
		mounted() {
			if (this.source) {
				this.currentUrl = this.makeUrl(this.source);
				http.load(this.currentUrl, this.$el).then(this.loaded);
			}
		},
		destroyed() {
			this.__destroy(); // and for dialogs too
		}
	});
})();