jQuery.fn.not_exists = function(){return jQuery(this).length==0;}

jQuery.fn.jqcollapse = function(o) {
 
 // Defaults
 var o = jQuery.extend( {
   slide: true,
   speed: 300,
   easing: ''
 },o);


 
 $(this).each(function(){
	 
	 var e = $(this).attr('id');
	
  	 $('#'+e+' li > a').each(function(i) {
		
		//alert($(this).attr("href"));
		
		// assigning the file name string to mainfile
		var mainfile = $(this).attr("href");
		// parsing for the file type
		var ext = mainfile.match(/(.*)\.([^(\. | \/)]+)(\/?)/)[2].toLowerCase();
		
		//alert(ext);
		
		//(/(.*)\.([^(\. | \/]+)(\/)$/)
		
		switch(ext){
			case "aac":
				$(this).parent().addClass('aacImageApply');
			break;
			case "ai":
				$(this).parent().addClass('aiImageApply');
			break;
			case "aiff":
				$(this).parent().addClass('aiffImageApply');
			break;
			case "avi":
				$(this).parent().addClass('aviImageApply');
			break;
			case "bmp":
				$(this).parent().addClass('bmpImageApply');
			break;
			case "c":
				$(this).parent().addClass('cImageApply');
			break;
			case "cpp":
				$(this).parent().addClass('cppImageApply');
			break;
			case "css":
				$(this).parent().addClass('cssImageApply');
			break;
			case "dat":
				$(this).parent().addClass('datImageApply');
			break;
			case "dmg":
				$(this).parent().addClass('dmgImageApply');
			break;
			case "dotx":
				$(this).parent().addClass('dotxImageApply');
			break;
			case "dwg":
				$(this).parent().addClass('dwgImageApply');
			break;
			case "dxf":
				$(this).parent().addClass('dxfImageApply');
			break;
			case "eps":
				$(this).parent().addClass('epsImageApply');
			break;
			case "exe":
				$(this).parent().addClass('exeImageApply');
			break;
			case "flv":
				$(this).parent().addClass('flvImageApply');
			break;
			case "h":
				$(this).parent().addClass('hImageApply');
			break;
			case "hpp":
				$(this).parent().addClass('hppImageApply');
			break;
			case "ics":
				$(this).parent().addClass('icsImageApply');
			break;
			case "iso":
				$(this).parent().addClass('isoImageApply');
			break;
			case "java":
				$(this).parent().addClass('javaImageApply');
			break;
			case "key":
				$(this).parent().addClass('keyImageApply');
			break;
			case "mid":
				$(this).parent().addClass('midImageApply');
			break;
			case "mp3":
				$(this).parent().addClass('mp3ImageApply');
			break;
			case "mp4":
				$(this).parent().addClass('mp4ImageApply');
			break;
			case "mpg":
				$(this).parent().addClass('mpgImageApply');
			break;
			case "odf":
				$(this).parent().addClass('odfImageApply');
			break;
			case "ods":
				$(this).parent().addClass('odsImageApply');
			break;
			case "odt":
				$(this).parent().addClass('odtImageApply');
			break;
			case "otp":
				$(this).parent().addClass('otpImageApply');
			break;
			case "ots":
				$(this).parent().addClass('otsImageApply');
			break;
			case "ott":
				$(this).parent().addClass('ottImageApply');
			break;
			case "php":
				$(this).parent().addClass('phpImageApply');
			break;
			case "ppt":
				$(this).parent().addClass('pptImageApply');
			break;
			case "psd":
				$(this).parent().addClass('psdImageApply');
			break;
			case "py":
				$(this).parent().addClass('pyImageApply');
			break;
			case "qt":
				$(this).parent().addClass('qtImageApply');
			break;
			case "rar":
				$(this).parent().addClass('rarImageApply');
			break;
			case "rb":
				$(this).parent().addClass('rbImageApply');
			break;
			case "rtf":
				$(this).parent().addClass('rtfImageApply');
			break;
			case "sql":
				$(this).parent().addClass('sqlImageApply');
			break;
			case "tga":
				$(this).parent().addClass('tgaImageApply');
			break;
			case "tgz":
				$(this).parent().addClass('tgzImageApply');
			break;
			case "tiff":
				$(this).parent().addClass('tiffImageApply');
			break;
			case "txt":
				$(this).parent().addClass('txtImageApply');
			break;
			case "wav":
				$(this).parent().addClass('wavImageApply');
			break;
			case "xls":
				$(this).parent().addClass('xlsImageApply');
			break;
			case "xlsx":
				$(this).parent().addClass('xlsxImageApply');
			break;
			case "xml":
				$(this).parent().addClass('xmlImageApply');
			break;
			case "yml":
				$(this).parent().addClass('ymlImageApply');
			break;
			case "zip":
				$(this).parent().addClass('zipImageApply');
			break;
			case "_page":
				$(this).parent().addClass('_pageImageApply');
			break;
			
			
			case "doc":
			case "docx":
				$(this).parent().addClass('docImageApply');
			break;
			case "pdf":
				$(this).parent().addClass('pdfImageApply');
			break;
			case "jpg":
				$(this).parent().addClass('jpgImageApply');
			break;
			case "png":
				$(this).parent().addClass('pngImageApply');
			break;
			case "gif":
				$(this).parent().addClass('gifImageApply');
			break;
			case "com":
			case "html":
			case "edu":
			case "org":
			case "gov":
				$(this).parent().addClass('webImageApply');
			break;
			default:
				$(this).parent().addClass('defaultImageApply');
			break;
		};
		
	});

	 $('#'+e+' li > ul').each(function(i) {
	    var parent_li = $(this).parent('li');
	    var sub_ul = $(this).remove();
	    
	    // Create 'a' tag for parent if DNE

	    if (parent_li.children('a').not_exists()) {
	    	parent_li.wrapInner('<a/>');
	    }
	    
		parent_li.addClass('closedImageApply');
	    parent_li.find('a').addClass('jqcNode').css('cursor','pointer').click(function() {
			
			// Toggle folder images
			if( $(this).parent().hasClass('closedImageApply')) {
				$(this).parent().removeClass('closedImageApply');
				$(this).parent().addClass('openImageApply');				
			} else {
				$(this).parent().removeClass('openImageApply');
				$(this).parent().addClass('closedImageApply');				
			}
			
	        if(o.slide==true){
	        	sub_ul.slideToggle(o.speed, o.easing);
	        }else{
	        	sub_ul.toggle();
	        }
	    });
	    parent_li.append(sub_ul);
	});
	
	//Hide all sub-lists
	 $('#'+e+' ul').hide();
	 
 });
 
};