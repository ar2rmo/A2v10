﻿// Copyright © 2015-2017 Alex Kukhtin. All rights reserved.

// 20171031-7064
// components/control.js

(function () {

	const control = {
		props: {
			label: String,
			required: Boolean,
			align: { type: String, default: 'left' },
			description: String,
			disabled: Boolean,
            tabIndex: Number
        },
        computed: {
			path() {
                return this.item._path_ + '.' + this.prop;
            },
            pathToValidate() {
                return this.itemToValidate._path_ + '.' + this.propToValidate;
            },
            errors() {
                if (!this.item) return null;
				let root = this.item._root_;
				if (!root) return null;
				if (!root._validate_)
                    return null;
                let err;
                if (this.itemToValidate)
                    err = root._validate_(this.itemToValidate, this.pathToValidate, this.itemToValidate[this.propToValidate], this.deferUpdate);
                else
                    err = root._validate_(this.item, this.path, this.item[this.prop], this.deferUpdate);
                return err;
            },
            inputClass() {
                let cls = '';
                if (this.align !== 'left')
                    cls += 'text-' + this.align;
                //if (this.dataType == 'Number' || this.dataType === N'Currency') {
                //  if (this.itemValue < 0)
                //      cls += ' negative-red';
                //}
                return cls;
			},
			hasLabel() {
				return !!this.label;
			},
			hasDescr() {
				return !!this.description;
			}
        },
        methods: {
            valid() {
                // method! no cache!
                return !this.invalid();
            },
            invalid() {
                // method! no cache!
                let err = this.errors;
                if (!err) return false;
                return err.length > 0;
            },
            cssClass() {
                // method! no cached!!!
                let cls = 'control-group' + (this.invalid() ? ' invalid' : ' valid');
                if (this.required) cls += ' required';
                if (this.disabled) cls += ' disabled';
                return cls;
            },
            deferUpdate() {
                this.$children.forEach((val) => val.$forceUpdate());
                this.$forceUpdate();
            },
            test() {
                alert('from base control');
            }
        }
    };

    app.components['control'] = control;

})();