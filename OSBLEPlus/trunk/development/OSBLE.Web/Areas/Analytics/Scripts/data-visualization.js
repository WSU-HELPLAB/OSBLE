if (typeof (DataVisualization) == "undefined") {
    var DataVisualization = {

        Init: function () {

            this.TimeFromElm().datetimepicker();
            this.TimeFromElm().on("dp.change", function(e) {
                this.TimeFromElm().data("DateTimePicker").minDate(e.date);
            });
            this.TimeToElm().datetimepicker();
            this.TimeToElm().on("dp.change", function (e) {
                this.TimeToElm().data("DateTimePicker").maxDate(e.date);
            });

            this.SpinnerElm().height($(window).height() * .5).css("margin-top", $(window).height() * .25);
            this.WireupEventHandlers();
            this.SetChartVisibility(false);

            $("#timescale-setting").val($("#timescale-setting").children(":selected").val());
        },

    SetChartVisibility: function (isVisible) {

        if (isVisible) {
            this.SpinnerElm().hide();
            this.ChartArea().fadeIn();
        }
        else {
            this.ChartArea().hide();
            this.SpinnerElm().show();
        }
    },

    WireupEventHandlers: function () {
        var self = this;

        $("#grayscale").change(function () {
            self.UpdateColorScale();
        });

        // redraw chart
        $("a.btn").click(function(e) {
            e.stopPropagation();
            e.preventDefault();

            self.SetChartVisibility(false);
            Chart.Draw();
        });

        $("#download").click(function (e) {
            e.stopPropagation();
            e.preventDefault();
            $(this).closest("form").submit();
        });
    },

    UpdateColorScale: function () {
        var self = this;

        var chartElms = $("rect"), grayscale = " grayscale", cls = "class";
        var runElms = $("rect.run, rect.debug, rect.edit").siblings(".rect-label");

        if (self.GrayScaleElm().prop("checked") === true) {
            chartElms.each(function(i, e) {
                var newClass = $(this).attr(cls) + grayscale;
                $(this).attr(cls, newClass);
            });
            runElms.each(function (i, e) {
                $(this).css("fill", "#fff");
            });
            self.LegendElm().addClass(grayscale);
        }
        else {
            chartElms.each(function(i, e) {
                var newClass = $(this).attr(cls).replace(grayscale, "");
                $(this).attr(cls, newClass);
            });
            runElms.each(function (i, e) {
                $(this).css("fill", "#333");
            });
            self.LegendElm().removeClass(grayscale);
        }
    },

    UpdateLegend: function () {

        var legendHR = $("div[data-type='legend-hour-view']");
        var legendMIN = $("div[data-type='legend-minute-view']");

        if (DataVisualization.TimeScale() == 3) {
            legendHR.hide();
            legendMIN.show();
        }
        else {
            legendHR.show();
            legendMIN.hide();
        }
    },

    TimeScale: function () {

        return $("#timescale-setting").val();
    },

    TickLabel: function () {
        var lbl = DataVisualization.TimeScale() == 1 ? " (day)" : (DataVisualization.TimeScale() == 2 ? " (hour)" : " (min)");
        return "<span class='tick-label'>Time " + lbl + " <span class='arrow'><span class='line'></span><span class='point'></span></span></span>";
    },

    ChartArea: function () {
        return $("div[data-type='chart-area']");
    },

    GrayScaleElm: function () {
        return $("#grayscale");
    },

    LegendElm: function () {
        return $("div.legend");
    },

    SpinnerElm: function () {

        return $("div[data-type='spinner']");
    },

    TimeFromElm: function () {

        return $("#timeFrom");
    },

    TimeToElm: function () {

        return $("#timeTo");
    },

    TimeVal: function (timeElm) {

        var t = timeElm.val();
        if (t.length > 0) {
            return new Date(t);
        }

        return new Date();
    },

    TimeString: function (time) {
        var str = time.toString("yyyy-MM-dd HH:mm");
        return str.substring(0, str.lastIndexOf(":"));
    },

    TimeRangeInHours: function () {
        return parseInt((this.TimeVal(this.TimeFromElm()).getTime() - this.TimeVal(this.TimeToElm()).getTime()) / (3600 * 1000));
    },
    };
}

if (typeof (Chart) == "undefined") {
    var Chart = (function () {

        return {

            showTooltip: function (evt) {
                var tooltip = evt.target.parentElement.getElementsByClassName("tooltipT")[0];
                tooltip.setAttributeNS(null, "x", $("#chartBody").scrollLeft() + evt.pageX - $("#chartBody").offset().left - 100);
                tooltip.setAttributeNS(null, "y", 8);
                tooltip.setAttributeNS(null, "visibility", "visible");
            },

            hideTooltip: function (evt) {
                var tooltip = evt.target.parentElement.getElementsByClassName("tooltipT")[0];
                tooltip.setAttributeNS(null, "visibility", "hidden");
            },

            showTooltipTK: function (evt) {
                var tooltip = evt.target.previousElementSibling;
                tooltip.setAttributeNS(null, "visibility", "visible");
                evt.target.setAttributeNS(null, "visibility", "hidden");
            },

            hideTooltipTK: function (evt) {
                var tooltip = evt.target.nextElementSibling;
                tooltip.setAttributeNS(null, "visibility", "visible");
                evt.target.setAttributeNS(null, "visibility", "hidden");
            },

            Draw: function () {

                // update data from the server

                    
                var dataservice = $("#main").attr("data-service-path");
                //dataservice = "http://localhost:1271";
                var checkedValues = $("input[name='userId']:checked").map(function () {
                    return this.value;
                }).get().join(",");
                alert(checkedValues);
                $.ajax({
                    url: dataservice + "/api/timeline",
                    //xhrFields: { withCredentials: true },
                    type: "GET",
                    headers: { "Access-Control-Allow-Origin": dataservice + ".*" },
                    data: {
                        timeScale: $("#timescale-setting").val(),
                        timeFrom: $("#timeFrom").val(),
                        timeTo: $("#timeTo").val(),
                        timeout: $("#timeout").val(),
                        grayscale: $("#grayscale").is(":checked"),
                        courseId: parseInt($("[data-course-id]").first().attr("data-course-id")),
                        userIds: checkedValues
            }
 }).done(function (data) {

                    // no chart data available show message
                    if (data.length === 0) {
                        $("#chart-area").text("No data in the time range!");
                        DataVisualization.SetChartVisibility(true);
                        return;
                    }

                    // redraw chart with new data
                    d3.select("svg").remove();
                    $("#chartBody tbody").empty();

                    // prepare linear scales for d3
                    var t = data[0];
                    var len = t.measures[t.measures.length - 1].endPoint;
                    var scale = 100;
                    var numTicks = len;
                    var tickScale = 1;

                    if ($("#timeFrom").val().length === 0) {
                        $("#timeFrom").val(t.measures[0].startTimeDisplayText);
                    }

                    if ($("#timeTo").val().length === 0) {
                        $("#timeTo").val(t.measures[t.measures.length - 1].endTimeDisplayText);
                    }

                    if (DataVisualization.TimeScale() == 1) {
                        // day view
                        scale = 500 / (60 * 24);
                        numTicks = len / (60 * 24);
                        tickScale = 24 * 60;
                    }
                    else if (DataVisualization.TimeScale() == 2) {
                        // hour view
                        scale = 500 / 60;
                        numTicks = len / 60;
                        tickScale = 60;
                    }
                    var wid = len * scale;

                    // instantiate svg drawing instance
                    var width = $(window).width() * 2 / 3, height = 65;
                    var margin = { top: 0, right: 100, bottom: 10, left: 0 },
                        width = width - margin.left - margin.right,
                        height = height - margin.top - margin.bottom;
                    var chart = d3.bullet().width(width).height(height).timeScale(scale).numTicks(numTicks).tickScale(tickScale);

                    // create a tr for each user
                    var rows = d3.select("#chartBody tbody")
                                 .selectAll("tr")
                             .data(data)
                                 .enter()
                                 .append("tr");

                    // add statistic names to each row
                    var tds = rows.each(function (r) {
                        d3.select(this)
                            .selectAll("td")
                     .data(function () { return [r.userId, ""]; })
                            .enter()
                            .append("td")
                        .text(function (d) { return d; });
                    });

                    // add svg to the second td of each row
                    var svgs = rows.each(function (r) {
                        d3.select(this).select("td:last-child")
                            .selectAll("svg")
                     .data(function () { return [r]; })
                            .enter()
                            .append("svg")
                            .attr("class", "bullet")
                            .attr("width", wid + 70)
                            .attr("height", height)
                            .append("g")
                        .attr("transform", "translate(30, 0)")
                            .call(chart)
                    });

                    // add fixed column styles
                    rows.each(function (r) {
                        d3.select(this).select("td:first-child").attr("class", "headcol").attr("title", r.title);
                        d3.select(this).select("td:last-child").attr("class", "col-xs-12");
                    });

                    // update tick labels (since each g inside bullet has no idea on timescales)
                    var $tickNode = $("#chartBody tbody").find("td.headcol:last");
                    if ($tickNode.children().length > 0) $tickNode.children().remove();
                    $tickNode.append(DataVisualization.TickLabel);

                    if (DataVisualization.TimeScale() == 3) {
                        //only show social event labels in minute view
                        setTimeout(function () {
                            $("svg").find("line.marker").each(function(index, value) {
                                var el = document.createElementNS("http://www.w3.org/2000/svg", "text");
                                el.setAttribute("x", parseFloat($(this).attr("x1")));
                                el.setAttribute("y", parseFloat($(this).attr("y1")) - 1);
                                el.textContent = $(this).attr("data-label");
                                $(this).after(el);
                            });
                        }, 500);
                    }

                    // hide wait spinner
                    DataVisualization.SetChartVisibility(true);
                    DataVisualization.UpdateLegend();
                });
            }
        };

    })();
}
