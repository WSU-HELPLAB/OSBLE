(function () {
    function cellInMeasureRange(month, day, measure) {
        return month === measure.firstDataPointMonth && day >= measure.firstDataPointDay
            || month === measure.lastDataPointMonth && day <= measure.lastDataPointDay;
    }

    function getValueFor(aggregates, monthA, dayA) {

        var aggregate = $.grep(aggregates, function (d, i) {
            return d.month === monthA && d.day === dayA;
        });

        if (aggregate.length > 0)
            return aggregate[0].value;

        // need to fill 0 value for missing days in between the first and last data points of a measure
        return 0;
    }

    function getAllDataPointsForDay(month, day, measures) {

        var dayData = [];
        if (measures != null) {
            $.each(measures, function (i, m) {
                if (cellInMeasureRange(month, day, m))
                    dayData.push({ title: m.title, value: getValueFor(m.aggregates, month, day), min: m.min, max: m.max });
            });
        }
        return dayData;
    }

    function combineCellPositionsWithDate(cellPositions, inputData, inactiveCellColor, activeCellColorC, activeCellColorN) {

        var year = inputData.year, month = inputData.month, measures = inputData != null ? inputData.measures : null;

        // days in the previous month
        var prevDate = new Date(year, month, 0);
        var prevMonth = prevDate.getMonth(),
            lastDayInPrevMonth = prevDate.getDate();
        var firstDayInWeek = new Date(year, month, 1).getDay(); // the day in the week of the first day of the display month

        // cell id tracking
        var cellPosition = 0;

        // days in the previous inactive month
        var i;
        for (i = 1; i <= firstDayInWeek; i++) {
            cellPositions[cellPosition]["month"] = prevMonth;
            cellPositions[cellPosition]["day"] = lastDayInPrevMonth - firstDayInWeek + i;
            cellPositions[cellPosition]["color"] = inactiveCellColor;
            cellPosition++;
        }

        // days in the first display month
        var daysInFirstMonth = new Date(year, month + 1, 0).getDate();
        for (i = 1; i <= daysInFirstMonth; i++) {
            cellPositions[cellPosition]["year"] = year;
            cellPositions[cellPosition]["month"] = month;
            cellPositions[cellPosition]["day"] = i;
            cellPositions[cellPosition]["color"] = activeCellColorC;
            cellPositions[cellPosition]["data"] = getAllDataPointsForDay(month, i, measures);
            cellPosition++;
        }

        // days in the second display month
        var secDate = new Date(year, month + 1, 1);
        var secMonth = secDate.getMonth(),
            daysInSecMonth = new Date(month === 11 ? year + 1 : year, month + 2, 0).getDate();
        for (i = 1; i <= daysInSecMonth; i++) {
            cellPositions[cellPosition]["year"] = secDate.getFullYear();
            cellPositions[cellPosition]["month"] = secMonth;
            cellPositions[cellPosition]["day"] = i;
            cellPositions[cellPosition]["color"] = activeCellColorN;
            cellPositions[cellPosition]["data"] = getAllDataPointsForDay(secMonth, i, measures);
            cellPosition++;
        }

        // days in the next inactive month
        var suffixDate = new Date(year, month + 2, 1);
        var suffixMonth = suffixDate.getMonth();
        var daysRequiredFromNextMonth = cellPositions.length - cellPosition;
        for (i = 1; i <= daysRequiredFromNextMonth; i++) {
            cellPositions[cellPosition]["month"] = suffixMonth;
            cellPositions[cellPosition]["day"] = i;
            cellPositions[cellPosition]["color"] = inactiveCellColor;
            cellPosition++;
        }
    }

    function getPositionId(cellPositions, monthP, dayP) {

        var position = $.grep(cellPositions, function (d, i) {
            return d.month === monthP && d.day === dayP;
        });

        return position[0].id;
    }

    d3.trendingCalendar = function () {

        var width = 600,
            height = 300,
            padding = 10,
            headingHeight = 40,
            inactiveCellColor = "#ccc",
            activeCellColorC = "#eee",
            activeCellColorN = "#ddd",
            onDayClick = function (year, month, day) { alert("You've clicked on " + month + "-" + day + ", " + year); },
            daysOfTheWeek = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

        var rowCount = 10, colCount = 7;

        function trendingCalendar(g) {

            // calendar grid attributes
            var gridwidth = width - padding,
                gridheight = height - headingHeight,
                cellwidth = gridwidth / colCount,
                cellheight = gridheight / rowCount;

            // calendar grid cell positions
            var cellPositions = [];
            for (var idxy = 0; idxy < rowCount; idxy++) {
                for (var idxx = 0; idxx < colCount; idxx++) {
                    cellPositions.push({ id: idxy * colCount + idxx, pos: [idxx * cellwidth, idxy * cellheight] });
                }
            }

            var tip = d3.tip()
              .attr("class", "d3-tip")
              .offset([10, 0])
              .html(function (d) {

                    var tips = [];
                    $.each(d.data, function(k, val) {
                        tips.push(val.title + ": " + val.value + "<br/>min: " + val.min + "<br/>max: " + val.max);
                    });

                    if (tips.length > 0)
                        return "<strong>" + tips.join("<br/><br/>") + "</strong>";

                    return "";
              });

            g.call(tip);

            g.each(function(inputData) {

                combineCellPositionsWithDate(cellPositions, inputData, inactiveCellColor, activeCellColorC, activeCellColorN);

                // append calendar grid cells to DOM
                g.selectAll("rect").data(cellPositions).enter().append("rect")
                    .attr("x", function(d) { return d.pos[0]; })
                    .attr("y", function(d) { return d.pos[1]; })
                    .attr("id", function(d) { return d.id; })
                    .attr("width", cellwidth)
                    .attr("height", cellheight)
                    .style("stroke", "#555")
                    .attr("transform", "translate(" + padding + "," + headingHeight + ")");

                // append calendar weekday heading to DOM
                g.selectAll("headers").data([0, 1, 2, 3, 4, 5, 6]).enter().append("text")
                    .attr("x", function(d) { return cellPositions[d].pos[0]; })
                    .attr("y", function(d) { return cellPositions[d].pos[1]; })
                    .attr("dx", padding)
                    .attr("dy", headingHeight - 10)
                    .text(function(d) { return daysOfTheWeek[d]; });

                // shade and label calendar cells
                g.selectAll("rect").data(cellPositions).style("fill", function(d) { return d.color; });
                g.select("g").select("text").remove(); // clear previously labeled days text
                g.append("svg:g").selectAll("daysText").data(cellPositions).enter().append("text")
                    .attr("x", function(d, i) { return d.pos[0]; })
                    .attr("y", function(d, i) { return d.pos[1]; })
                    .attr("dx", padding) // right padding
                    .attr("dy", 2 * padding) // vertical alignment : middle
                    .attr("transform", "translate(" + padding + "," + headingHeight + ")")
                    .text(function(d) { return d.day; });

                /************************************************************************************
                 ********  trend charts: data points + paths
                 ************************************************************************************/

                // clear previous data points
                //g.selectAll("text[text-anchor]").remove();
                g.selectAll("circle").remove();
                g.selectAll("path").remove();

                // loop through measures to append data points and paths
                for (var m = 0; m < inputData.measures.length; m++) {

                    // measure data
                    var measure = inputData.measures[m];

                    // scale based on each measure's data range
                    var y = d3.scale.linear().domain([0, measure.max + 1]).range([0, cellheight]);

                    // chart data points and dynamic tooltips
                    //g.append("svg:g").selectAll("dataPoint").data(cellPositions).enter().append("text")
                    //                 .text(function (d) { return measure.dataPointShape; })
                    //                 .attr("text-anchor", function (d, i) { return cellInMeasureRange(d.month, d.day, measure) ? "middle" : null; })
                    //                 .attr("x", function (d, i) { return cellInMeasureRange(d.month, d.day, measure) ? d.pos[0] + cellwidth / 2 + padding : null; })
                    //                 .attr("y", function (d, i) { return cellInMeasureRange(d.month, d.day, measure) ? d.pos[1] + cellheight + headingHeight - y(getValueFor(measure.aggregates, d.month, d.day)) : null; })
                    //                 .style("fill", function (d) { return measure.color; });

                    g.append("svg:g").selectAll("dataPoint").data(cellPositions).enter().append("circle")
                        .attr("cx", function(d, i) { return cellInMeasureRange(d.month, d.day, measure) ? d.pos[0] + cellwidth / 2 + padding : null; })
                        .attr("cy", function(d, i) { return cellInMeasureRange(d.month, d.day, measure) ? d.pos[1] + cellheight + headingHeight - y(getValueFor(measure.aggregates, d.month, d.day)) : null; })
                        .attr("r", function(d, i) { return cellInMeasureRange(d.month, d.day, measure) ? 3.5 : null; })
                        .style("fill", function(d) { return measure.color; });


                    // lines to connect the dots
                    var line = d3.svg.line()
                        .x(function(d, i) {
                            return cellInMeasureRange(d.month, d.day, measure) ? (d.id === -1 ? d.pos[0] + padding : (d.id === -2 ? d.pos[0] + cellwidth + padding : d.pos[0] + cellwidth / 2 + padding)) : null;
                        })
                        .y(function(d, i) {
                            return cellInMeasureRange(d.month, d.day, measure) ? d.pos[1] + cellheight + headingHeight - (d.id < 0 ? y(d.data) : y(getValueFor(measure.aggregates, d.month, d.day))) : null;
                        })
                        .interpolate("linear");
                    // data slices for each line segments
                    var firstCellId = getPositionId(cellPositions, measure.firstDataPointMonth, measure.firstDataPointDay);
                    var lastCellId = getPositionId(cellPositions, measure.lastDataPointMonth, measure.lastDataPointDay);

                    for (var idx = Math.floor(firstCellId / colCount); idx < Math.ceil(lastCellId / colCount); idx++) {

                        // for each calendar row
                        var leftEndPoint = Math.max(idx * colCount, firstCellId);
                        var rightEndPoint = Math.min((idx + 1) * colCount, lastCellId + 1);
                        var dataSlice = cellPositions.slice(leftEndPoint, rightEndPoint);
                        if (leftEndPoint !== firstCellId) {

                            // get the cell data index, it varies from cell to cell
                            var dataindex = $.map(cellPositions[leftEndPoint].data, function(md, mi) {
                                if (md.title === measure.title) {
                                    return mi;
                                }
                            })[0];

                            var dupcell = cellPositions[leftEndPoint];
                            var mleftVal = (dupcell.data[dataindex].value + cellPositions[leftEndPoint - 1].data[dataindex].value) / 2;
                            var mleft = { id: -1, day: dupcell.day, month: dupcell.month, pos: dupcell.pos, data: mleftVal };
                            dataSlice.splice(0, 0, mleft);
                        }
                        if (rightEndPoint < lastCellId) {

                            // get the cell data index, it varies from cell to cell
                            var dataindex = $.map(cellPositions[rightEndPoint].data, function(md, mi) {
                                if (md.title === measure.title) {
                                    return mi;
                                }
                            })[0];

                            var dupcell = cellPositions[rightEndPoint - 1];
                            var mrightVal = (dupcell.data[dataindex].value + cellPositions[rightEndPoint].data[dataindex].value) / 2;
                            var mright = { id: -2, day: dupcell.day, month: dupcell.month, pos: dupcell.pos, data: mrightVal };
                            dataSlice.splice(dataSlice.length, 0, mright);
                        }
                        g.append("svg:g").append("svg:path")
                            .attr("d", line(dataSlice))
                            .style("stroke", measure.color)
                            .style("pointer-events", "all").append("title").text(function(d) { return measure.title; }); // tooltip
                    }

                    // attaching hover and click handlers to the data cells
                    cellPositions.forEach(function(d) {

                        if (cellInMeasureRange(d.month, d.day, measure)) {

                            d3.select("rect[id='" + d.id + "']")
                                .on("mouseover", tip.show)
                                .on("mouseout", tip.hide)
                                .on("click", function(d) {
                                    onDayClick(d.year, d.month, d.day, true);
                                });
                        }
                    });
                }

                // events
                g.append("svg:g").selectAll("eventText").data(inputData.activities).enter().append("text")
                    .attr("x", function(d, i) {
                        return cellPositions[getPositionId(cellPositions, d.month, d.day)].pos[0];
                    })
                    .attr("y", function(d, i) {
                        return cellPositions[getPositionId(cellPositions, d.month, d.day)].pos[1];
                    })
                    .attr("dx", padding) // right padding
                    .attr("dy", cellheight / 2) // vertical alignment : middle
                    .attr("transform", "translate(" + padding + "," + headingHeight + ")")
                    .text(function(d) { return d.name; })
                    .call(wrap, cellwidth, (rowCount - 3.5) * cellheight, padding);
            });
        }

        trendingCalendar.width = function (x) {
            if (!arguments.length) return width;
            width = x;
            return trendingCalendar;
        };

        trendingCalendar.height = function (x) {
            if (!arguments.length) return height;
            height = x;
            return trendingCalendar;
        };

        trendingCalendar.padding = function (x) {
            if (!arguments.length) return padding;
            padding = x;
            return trendingCalendar;
        };

        trendingCalendar.headingHeight = function (x) {
            if (!arguments.length) return headingHeight;
            headingHeight = x;
            return trendingCalendar;
        };

        trendingCalendar.inactiveCellColor = function (x) {
            if (!arguments.length) return inactiveCellColor;
            inactiveCellColor = x;
            return trendingCalendar;
        };

        trendingCalendar.activeCellColorC = function (x) {
            if (!arguments.length) return activeCellColorC;
            activeCellColorC = x;
            return trendingCalendar;
        };

        trendingCalendar.activeCellColorN = function (x) {
            if (!arguments.length) return activeCellColorN;
            activeCellColorN = x;
            return trendingCalendar;
        };

        trendingCalendar.onDayClick = function (x) {
            if (!arguments.length) return onDayClick;
            onDayClick = x;
            return trendingCalendar;
        };

        return trendingCalendar;
    };

    function wrap(text, width, yoffset, xpadding) {
        text.each(function () {
            var tt = d3.select(this),
                words = tt.text().split(/\s+/).reverse(),
                word,
                line = [],
                lineNumber = 0,
                lineHeight = 1.1, // ems
                x = tt.attr("x"),
                y = tt.attr("y") - yoffset,
                dy = parseFloat(tt.attr("dy")),
                tspan = tt.text(null).append("tspan").attr("x", x).attr("y", y).attr("dx", xpadding).attr("dy", dy + "em");
            while (word = words.pop()) {
                line.push(word);
                tspan.text(line.join(" "));
                if (tspan.node().getComputedTextLength() > width) {
                    line.pop();
                    tspan.text(line.join(" "));
                    line = [word];
                    tspan = tt.append("tspan").attr("x", x).attr("y", y).attr("dx", xpadding).attr("dy", ++lineNumber * lineHeight + dy + "em").text(word);
                }
            }
        });
    }

})();
