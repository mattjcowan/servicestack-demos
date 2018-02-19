var riot = require('riot')

module.exports = function(callback, tagFile, todos) { 
    var tag = require(tagFile);
    var html  = riot.render(tag, {
        title: 'This is populated from server',
        items: todos
    })
    callback(null, html); 
};