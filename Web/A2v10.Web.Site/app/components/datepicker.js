﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

// 20180619-7227
// components/datepicker.js


(function () {

	const popup = require('std:popup');

	const utils = require('std:utils');
	const eventBus = require('std:eventBus');

	const baseControl = component('control');
	const locale = window.$$locale;

	Vue.component('a2-date-picker', {
		extends: baseControl,
		template: `
<div :class="cssClass2()" class="date-picker">
	<label v-if="hasLabel" v-text="label" />
	<div class="input-group">
		<input v-focus v-model.lazy="model" :class="inputClass" :disabled="disabled" />
		<a href @click.stop.prevent="toggle($event)"><i class="ico ico-calendar"></i></a>
		<validator :invalid="invalid" :errors="errors" :options="validatorOptions"></validator>
		<div class="calendar" v-if="isOpen">		
			<a2-calendar :model="modelDate"
				:set-month="setMonth" :set-day="selectDay" :get-day-class="dayClass"/>
		</div>
	</div>
	<span class="descr" v-if="hasDescr" v-text="description"></span>
</div>
`,
		props: {
			item: Object,
			prop: String,
			itemToValidate: Object,
			propToValidate: String,
			// override control.align (default value)
			align: { type: String, default: 'center' }
		},
		data() {
			return {
				isOpen: false
			};
		},
		methods: {
			toggle(ev) {
				if (!this.isOpen) {
					// close other popups
					eventBus.$emit('closeAllPopups');
					if (utils.date.isZero(this.modelDate))
						this.item[this.prop] = utils.date.today();
				}
				this.isOpen = !this.isOpen;
			},
			setMonth(dt) {
				this.item[this.prop] = dt;
			},
			selectDay(day) {
				this.item[this.prop] = day;
				this.isOpen = false;
			},
			dayClass(day) {
				let cls = '';
				let tls = utils.date;
				if (tls.equal(day, tls.today()))
					cls += ' today';
				if (tls.equal(day, this.modelDate))
					cls += ' active';
				if (day.getMonth() !== this.modelDate.getMonth())
					cls += " other";
				return cls;
			},
			cssClass2() {
				let cx = this.cssClass();
				if (this.isOpen)
					cx += ' open'
				return cx;
			},
			__clickOutside() {
				this.isOpen = false;
			}
		},
		computed: {
			modelDate() {
				return this.item[this.prop];
			},
			model: {
				get() {
					if (utils.date.isZero(this.modelDate))
						return '';
					return this.modelDate.toLocaleString(locale.$Locale, { year: 'numeric', month: '2-digit', day: '2-digit' });
				},
				set(str) {
					let md = utils.date.parse(str);
					this.item[this.prop] = md;
					if (utils.date.isZero(md))
						this.isOpen = false;
				}
			}
		},
		mounted() {
			popup.registerPopup(this.$el);
			this.$el._close = this.__clickOutside;
		},
		beforeDestroy() {
			popup.unregisterPopup(this.$el);
		}
	});
})();
