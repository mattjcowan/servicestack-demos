(function(d) {
    function ping(done) {
        var success = done;
        // delay 
        setTimeout(function() {
                var repeat = function() {
                    setTimeout(function() {
                        ping(success);
                    }, 1000);
                };
                $.ajax({
                        type: 'GET',
                        url: '/api/ping',
                        contentType: 'application/json',
                        data: '',
                        dataType: 'json',
                        timeout: 2000
                    })
                    .done(function(data) {
                        if (data.serverTime) {
                            location.href = '/';
                        } else {
                            repeat();
                        }
                    })
                    .fail(function() {
                        repeat();
                    });
            },
            2000);
    }
    function restart() {
        $.ajax({
                type: "POST",
                url: "/api/restart",
                contentType: 'application/json',
                data: JSON.stringify({}),
                dataType: 'json'
            })
            .always(function() {
                // wait until the server is back up and then redirect
                ping(function() {
                    location.href = '/';
                });
            });
    }
    $(d)
        .ready(function() {
            $('input.submit')
                .on('click',
                    function() {
                        $.ajax({
                                type: "POST",
                                url: "/api/db",
                                contentType: 'application/json',
                                data: JSON.stringify({
                                    connectionString: $("textarea").val(),
                                    dialect: $("input[name='dialect']:checked").val()
                                }),
                                dataType: 'json'
                            })
                            .done(function(data) {
                                if (data.success) {
                                    $(".alert").hide();
                                    $(".input-section").hide();
                                    $(".alert-success").show();
                                    restart();
                                } else {
                                    $(".alert").hide();
                                    $(".alert-warning").show();
                                    $(".error-message").text(data.message);
                                }
                            })
                            .fail(function(err) {
                                $(".alert").hide();
                                $(".alert-warning").show();
                                $(".error-message").text(err.responseJSON.responseStatus.message);
                            });
                    });
        });
})(document);