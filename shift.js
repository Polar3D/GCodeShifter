#!/usr/bin/env node
var fs = require('fs');
var path = require('path');
var Math = require('math');
var shift = 0;
var foundZ1 = false;
var foundZ2 = false;
var Angle = 35;
var Layer = .3;
var foundM117 = false;

var z_offset = Layer / Math.cos(Angle * (Math.PI / 180)) - Layer;
var x_offset = Layer * Math.tan(Angle * (Math.PI / 180));

var _ = require('lodash');
var parser = require('gcode-parser');

var fileName = process.argv[2];
 
parser.parseFile(fileName, function(err, results) {
    if (err) {
        console.error(err);
        return;
    }

    var myResults = _(results)
	.map('words')
	.map(function(words) {
		return _.map(words, function(word) {
			
			if(foundM117)
			{
				if(word[0]==="Z")
				{
					if(foundZ1)
					{
						foundZ2=true;
					}
					else
					{
						foundZ1=true;
					}
				}

				if(word[0]==="X")
				{
					if(!foundZ2)
					{
						if(shift!=0)
						{
							var contender = word[1];
							if(shift>contender)
							{
						 		shift = contender;
							}							
						}
						else
						{
					 		shift = word[1];
						}
					}
				}
			}

			if(word[0]==="M" && word[1]===117) foundM117 = true;

			return word[0] + word[1];
		}).join(' ');
	})
	.value();

    foundM117 = false;
 
    // Compose G-code 
    var list = _(results)
	.map('words')
        .map(function(words) {
            return _.map(words, function(word) {

		if(foundM117)
		{

			if(word[0]==="Z")
			{
				Layer = word[1];
				z_offset = Layer / Math.cos(Angle * (Math.PI / 180)) - Layer;
				word[1]=(word[1]+z_offset).toFixed(5);
			}
			if(word[0]==="X")
			{
				x_offset = Layer * Math.tan(Angle * (Math.PI / 180));
				word[1]=(word[1]+x_offset-shift).toFixed(5);
			}

		}

		if(word[0]==="M" && word[1]===117) foundM117 = true;

                return word[0] + word[1];

            }).join(' ');
        })
        .value();

    var output = list.toString().replace(/,/g, '\n');
    process.stdout.write(output);
    process.stdout.write('\n'); 
})
