﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

// 20190122-7421
// components/upload.js



(function () {

	const url = require('std:url');
	const http = require('std:http');

	const locale = window.$$locale;

	Vue.component("a2-upload", {
        /* TODO:
         4. ControllerName (_image ???)
        */
		template: `
<label :class="cssClass" @dragover="dragOver" @dragleave="dragLeave">
	<input v-if='canUpload' type="file" @change="uploadImage" v-bind:multiple="isMultiple" :accept="accept" />
	<i class="ico" :class="icoClass"></i>
	<span class="upload-tip" v-text="tip" v-if="tip"></span>
</label>
		`,
		props: {
			item: Object,
			prop: String,
			base: String,
			newItem: Boolean,
			tip: String,
			readOnly: Boolean,
			accept: String
		},
		data: function () {
			return {
				hover: false
			};
		},
		computed: {
			cssClass() {
				return 'file-upload' + (this.hover ? ' hover' : '');
			},
			isMultiple() {
				return !!this.newItem;
			},
			canUpload() {
				return !this.readOnly;
			},
			icoClass() {
				return this.accept === 'image/*' ? 'ico-image' : 'ico-upload';
			}
		},
		methods: {
			dragOver(ev) {
				this.hover = true;
				ev.preventDefault();
			},
			dragLeave(ev) {
				this.hover = false;
				ev.preventDefault();
			},
			uploadImage(ev) {
				let root = window.$$rootUrl;
				let id = this.item[this.prop];
				let imgUrl = url.combine(root, '_image', this.base, this.prop, id);
				var fd = new FormData();
				for (let file of ev.target.files) {
					fd.append('file', file, file.name);
				}
				http.upload(imgUrl, fd).then((result) => {
					// result = {status: '', ids:[]}
					ev.target.value = ''; // clear current selection
					if (result.status === 'OK') {
						// TODO: // multiple
						if (this.newItem) {
							let p0 = this.item.$parent;
							for (let id of result.ids) {
								let ni = p0.$append();
								ni[this.prop] = id;
							}
						} else {
							this.item[this.prop] = result.ids[0];
						}
					}
				}).catch(result => {
					alert(result);
				});
			}
		}
	});

	Vue.component("a2-simple-upload", {
		template: `
<label class="a2-simple-upload" :class="labelClass">
	<i class="ico" :class='icon'></i>
	<input type="file" @change="uploadChange" ref="file"/>
	<span v-text="labelText" class="upload-text"></span>
	<button class="btnclose" @click.prevent="clear" v-if="file">&#x2715;</button>
</label>
		`,
		props: {
			item: Object,
			prop: String,
			text: String
		},
		data: function () {
			return {
			};
		},
		computed: {
			labelText() {
				if (this.file) return this.file.name;
				return this.text || locale.$ChooseFile;
			},
			icon() {
				return this.file && this.file.name ? 'ico-file' : 'ico-attach';
			},
			file() {
				return this.item ? this.item[this.prop] : null;
			},
			labelClass() {
				return this.file ? 'has-file' : undefined;
			}
		},
		methods: {
			fireChange() {
			},
			clear() {
				if (!this.item) return;
				this.item[this.prop] = null;
				this.fireChange();
				this.$refs.file.value = '';
			},
			uploadChange(ev) {
				let files = ev.target.files;
				this.item[this.prop] = files[0];
				this.fireChange();
			}
		}
	});

})();
