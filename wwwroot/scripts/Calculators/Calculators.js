
ns4 = (document.layers)? true:false
ie4 = (document.all)? true:false
var kurs=EURKurs;

document.onkeydown = keyDown
if (ns4) document.captureEvents(Event.KEYDOWN)

var setProductLink = function() {
	$('#productLink').attr('href','#');
	var idx = SelectCalculator.value;
	for(var k in calcs) {
		if(calcs[k].Id==idx) {
			$('#productLink').attr('href',calcs[k].Link);
			break;
		}
	}
};setProductLink();

function keyDown(e) 
{
    
    if(!!e)
    {
        if(e.target.className !="numonly") return;
    }
    else
    {
        if(event.srcElement.className!="numonly") return;
    }


    var i = SelectCalculator.value;
    var errordivId = 'errordivCalc'+i;
    if (ns4) 
    {
        var nKey = e.which
        if (((nKey>31) && (nKey<45)) || ((nKey>57) && (nKey!=144) && (nKey!=190) && (nKey!=110) && ((nKey<96) || (nKey>105)))) 
       { 
            document.getElementById(errordivId).innerHTML = warningMsg11;
            return false;
        }
        else
        {
            document.getElementById(errordivId).innerHTML = "&nbsp;";
        }
    }
    if (ie4) 
    {
        var ieKey = event.keyCode
        if (((ieKey>31) && (ieKey<45)) || ((ieKey>57) && (ieKey!=144) && (ieKey!=190) && (ieKey!=110) && ((ieKey<96) || (ieKey>105)))) 
        {
            document.getElementById(errordivId).innerHTML = warningMsg11;
            return false;
        }
        else
        {
            document.getElementById(errordivId).innerHTML = "&nbsp;";
        }
    }
    else
    {
        var fKey = (e.which) ? e.which : e.keyCode
        if (((fKey>31) && (fKey<45)) || ((fKey>57) && (fKey!=144) && (fKey!=190) && (fKey!=110) && ((fKey<96) || (fKey>105)))) 
        {
            document.getElementById(errordivId).innerHTML = warningMsg11;
            return false;
        }
        else
        {
            document.getElementById(errordivId).innerHTML = "&nbsp;";
        }
    }
}


function select_calc()
{

	setProductLink();

	var i, j, tableId, currentId;
	i = SelectCalculator.value;

	if(i == 6)
	{
        document.getElementById('errordivCalc6').innerHTML = "&nbsp;";
        document.getElementById('kamataCalc6').value = "";
        document.getElementById('iznosCalc6').value = "";   

        var ddl6 = document.getElementById('periodCalc6');
        document.getElementById('valutaCalc6').innerHTML = mkd;
        ddl6.options.length = 1;
        var theOption = new Option;
        theOption.text = 4;
        theOption.value = 4;
        ddl6.options[0] = theOption;
        document.getElementById('nacinIsplataRed').style.display = "none";;
	}
    
    if(i == 21)
    {
        document.getElementById('errordivCalc21').innerHTML = "&nbsp;";
        document.getElementById('kamataCalc21').value = "";
        document.getElementById('iznosCalc21').value = "";  
        document.getElementById('valutaCalc21').innerHTML = "&#8364;";
    
        var ddl21 = document.getElementById('periodCalc21');
        ddl21.options.length = 2;
        var theOption = new Option;
        theOption.text = 3;
        theOption.value = 3;
        ddl21.options[0] = theOption;
        var theOption1 = new Option;
        theOption1.text = 6;
        theOption1.value = 6;
        ddl21.options[1] = theOption1;
        document.getElementById('nacinIsplataRed21').style.display = "table-row";
	}
	tableId = 'calc'+i;
	var myArray = [1,2,3,4,5,6,7,8,9,21, 20];
	for (var k = 0; k < myArray.length; k++) 
	{
	    j = myArray[k];
		currentId = 'calc'+j; 
		if(currentId == tableId)
		{
			document.getElementById(currentId).style.display = "block";
		}
		else
		{
		    document.getElementById(currentId).style.display = "none";
		    
		}
	}
    
    currency_change();
}

function reset_inputText()
{
	var inputElements = document.getElementsByTagName("input");
	for (var i = 0; i < inputElements.length; i++)
	{
		if(inputElements[i].type == "text")
		{
			inputElements[i].value = "";
		}
	}
}

function currency_change()
{
    var i=SelectCalculator.value;
    if(i == 1)
    {
        document.getElementById('errordivCalc1').innerHTML = "&nbsp;";
        document.getElementById('errordiv1Calc1').innerHTML = "&nbsp;";
        
        reset_inputText();
         if(document.getElementById('currencyCalc1').value=="1")
        {  
        document.getElementById('spanDenari').innerHTML = denari;
        document.getElementById('spanDenari2').innerHTML = denari;
       // document.getElementById('spanDenari3').innerHTML = denari;
       // document.getElementById('pZabeleskaCalc1').style= "visibility:visible;";
       }
       else
       {
        document.getElementById('spanDenari').innerHTML = "&#8364;";
        document.getElementById('spanDenari2').innerHTML = "&#8364;";
        //document.getElementById('spanDenari3').innerHTML = "&#8364;";
        //document.getElementById('pZabeleskaCalc1').style= "visibility:hidden;";
       }
    }
    else if(i == 2)
    {
        document.getElementById('errordivCalc2').innerHTML = "&nbsp;";
        
        reset_inputText();    
        if(document.getElementById('currencyCalc2').value=="1")
        {  
            document.getElementById('spanDenari7').innerHTML = denari;
            document.getElementById('spanDenari8').innerHTML = denari;
        }
        else
        {
            document.getElementById('spanDenari7').innerHTML = "&#8364;";
            document.getElementById('spanDenari8').innerHTML = "&#8364;";
        }
        //document.getElementById('spanDenari9').innerHTML = "&#8364;";        
    }
    else if(i == 3)
    {
        document.getElementById('errordivCalc3').innerHTML = "&nbsp;";
        
        reset_inputText();        
        document.getElementById('spanDenari4').innerHTML = denari;
        document.getElementById('spanDenari5').innerHTML = denari;
        document.getElementById('spanDenari6').innerHTML = denari;        
    }
    else if(i == 4)
    {
        document.getElementById('errordivCalc4').innerHTML = "&nbsp;";
        
        reset_inputText();        
        document.getElementById('spanDenari10').innerHTML = "&#8364;";
        document.getElementById('spanDenari11').innerHTML = "&#8364;";
        //document.getElementById('spanDenari12').innerHTML = "&#8364;";
       
    }
    else if(i == 5)
    {
        document.getElementById('errordivCalc10').innerHTML = "&nbsp;";
        document.getElementById('errordiv1Calc10').innerHTML = "&nbsp;";
        
        reset_inputText();
        
        document.getElementById('spanDenari13').innerHTML = "&#8364;";
        document.getElementById('spanDenari14').innerHTML = "&#8364;";
        document.getElementById('spanDenari15').innerHTML = "&#8364;";
        document.getElementById('spanDenari16').innerHTML = "&#8364;";
        document.getElementById('spanDenari19').innerHTML = "&#8364;";
        
    }
    else if(i == 7)
    {
        document.getElementById('errordivCalc7').innerHTML = "&nbsp;";
        
        reset_inputText();        
        document.getElementById('spanDenari17').innerHTML = "&#8364;";
        document.getElementById('spanDenari18').innerHTML = "&#8364;";        
    }
    else if(i == 10)
    {
        document.getElementById('errordivCalc10').innerHTML = "&nbsp;";
        document.getElementById('errordiv1Calc10').innerHTML = "&nbsp;";
        
        reset_inputText();
        
        document.getElementById('spanDenari20').innerHTML = "&#8364;";
        document.getElementById('spanDenari21').innerHTML = "&#8364;";
        document.getElementById('spanDenari23').innerHTML = "&#8364;";
        document.getElementById('spanDenari24').innerHTML = "&#8364;";
        document.getElementById('spanDenari22').innerHTML = "&#8364;";
        document.getElementById('spanDenari25').innerHTML = "&#8364;";
        document.getElementById('spanDenari26').innerHTML = "&#8364;";
        
    }
    
    document.getElementById('kamataCalc6').value = "";
    document.getElementById('iznosCalc6').value = "";
    var ddl6 = document.getElementById('periodCalc6');

    document.getElementById('kamataCalc21').value = "";
    document.getElementById('iznosCalc21').value = "";   
    var ddl21 = document.getElementById('periodCalc21');
	document.getElementById('valutaCalc21').innerHTML = "&#8364;";
}

function binomial(a, n)
{
  var i;
  var sum, pow, term, cof;
  if(n < 0)
    return 1.0 / binomial(a, -n);
  sum = 1.0;
  pow = n;
  term = 1;
  cof = 1.0
  for(i = 1; i < 10; i++) {
    cof = cof * pow / i;
    pow = pow - 1.0;
    term = term * a;
    sum = sum + cof * term;
  }
  return sum;
}

function error(P, I, N, Y, M)
{
  var value;
  value = P - M * (1.0 - binomial(I / Y, -N)) / (I / Y);
  return value;
}

/* Compute power */
function power(x,y)
{ total=1;
  for (j=0; j<y; j++)
   { total*=x; }
  return total; //result of x raised to y power
}

/* Compute Interest rate */
function compute_amountCalc1()
{
 	var P, R, N, K, M, T, Plt, Plata;
    var type = document.getElementById('currencyCalc1').value;

	if(document.getElementById('loanCalc1').value.length == 0)
		document.getElementById('amountCalc1').value = "0.00";
	else 
    {
    	Y = eval(document.getElementById('yearsCalc1').value);
    	
		P= eval(document.getElementById('KSCalc1').value);
		<!--Plt=0.35; -->	
		
        K = eval(document.getElementById('loanCalc1').value);
		if(type=="1")
		{
            Plt=kamata1;//Plt=0.35;
        }
		else
		{
            Plt=kamata2;//Plt=0.26;
            //K = eval(document.getElementById('loanCalc1').value)*EURKurs; 
        }
		
	    if(((type=='1')&&((K<15000) || (K>1500000) || (Y>10))) || ((type=='2')&&((K<250) || (K>25000) || (Y>10))) )
	    {
		    document.getElementById('errordivCalc1').innerHTML = warningMsg1;
            return false;
	    }
	    else
	    {
		    document.getElementById('errordivCalc1').innerHTML = "&nbsp;";
	    }

        if(type=='2')
            K=K*EURKurs;

		T = Y*12;
		M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K

//      	Plata = Math.round(M/Plt*100) / 100 ;
//		
//        if ((Plata<7700) && (K<=61000)) 
//		{
//		    Plata=7700;
//		}
//		if ((Plata<10200) && (K>61000)) 
//		{
//		    Plata=10200;
//		}

        if(type=='2') 
        {
			M=M/EURKurs;
//			Plata=Plata/EURKurs;
//            Plata = Math.round(Plata*100) / 100;
		}
		
		M = Math.round(M*100) / 100;
		
        //nikojpat nemoze da se dostigne minimalna mesecna rata(M)
//		if ( M<20)
//		{
//			//alert(warningMsg2);
//		    document.getElementById('errordiv1Calc1').innerHTML = warningMsg2;
//            return false;	
//		}
//		else
//		{
            if(isNaN(M))// && isNaN(Plata))
            {
		        document.getElementById('errordiv1Calc1').innerHTML = "&nbsp;";
    	        document.getElementById('amountCalc1').value = "";
//		        document.getElementById('plataCalc1').value = "";
            }
            else
            {
		        document.getElementById('errordiv1Calc1').innerHTML = "&nbsp;";
    	        document.getElementById('amountCalc1').value = "" + M;
//		        document.getElementById('plataCalc1').value = "" + Plata;
            }
//		}
  	}
}

function allowGrejsCalc2()
{
var curr=document.getElementById('currencyCalc2').value
}

function compute_amountCalc2()
{
 	var P, R, N, K, M, T, Plt, Plata;
 
    var type = document.getElementById('currencyCalc2').value;

	if(type == "1")
		{
        Plt=kamata3;//Plt=0.35;
        }

	if(type == "2")
		{
        Plt=kamata4;//Plt=0.26;} 
        }
		 
		 
	
	if(document.getElementById('loanCalc2').value.length == 0)  
		document.getElementById('amountCalc2').value = "0.00";
	else {
    	Y = eval(document.getElementById('yearsCalc2').value);
    	K = eval(document.getElementById('loanCalc2').value);
		P = eval(document.getElementById('KSCalc2').value);
	
    if(type == "1")
        K=K/EURKurs;

	if((K>80000) || (Y>20))
	{
		//alert(warningMsg1);
		//document.getElementById('errordivCalc2').style.display = "block";
		document.getElementById('errordivCalc2').innerHTML = warningMsg1;
        return false;
	}
	else
	{
	    document.getElementById('errordivCalc2').innerHTML = "&nbsp;";
	}

		T = Y*12;
		
		M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K;
			
		if (type == "1")
			{
			Plata = Math.round(M/Plt*100) / 100 ;
			if (Plata<125) Plata=125;
            M = M * EURKurs;
            Plata = Plata * EURKurs;
			}
		else
			{
			Plata = Math.round(M/Plt*100) / 100 ;
			if (Plata<167) Plata=167;
			}
					
		
		M = Math.round(M*100) / 100;

        if(isNaN(M) && isNaN(Plata))
        {
    	    document.getElementById('amountCalc2').value = "";
		    //document.getElementById('plataCalc2').value = "";
        }
        else
        {
    	    document.getElementById('amountCalc2').value = "" + M;
		    //document.getElementById('plataCalc2').value = "" + Plata;
        }
  	}
}

function compute_amountCalc3()
{
 	var P, R, N, K, M, T, Plt, Plata;
 
	
	if(document.getElementById('currencyCalc3').value=="1")
		{
        Plt=kamata5;//Plt=0.35;
        }

	if(document.getElementById('currencyCalc3').value=="2")
		{
        Plt=kamata6;//Plt=0.26;
        }

	if(document.getElementById('loanCalc3').value.length == 0)  
		document.getElementById('amountCalc3').value = "0.00";
	else 
    {
    	Y = eval(document.getElementById('yearsCalc3').value);
    	K = eval(document.getElementById('loanCalc3').value);
		P = eval(document.getElementById('KSCalc3').value);
	
	if((K<12000) || (K>300000) || (Y>72))
	{
		//alert(warningMsg1);
		//document.getElementById('errordivCalc3').style.display = "block";
		document.getElementById('errordivCalc3').innerHTML = warningMsg1;
        return false;
	}
	else
	{
	    document.getElementById('errordivCalc3').innerHTML = "&nbsp;";
	}

		T = Y;
		<!--K=K+1000-->
		M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K

      	Plata = Math.round(M/Plt*100) / 100
		if(Plata<=7700) Plata=7700;
		M = Math.round(M*100) / 100;

        if(isNaN(M) && isNaN(Plata))
        {
    	    document.getElementById('amountCalc3').value = "";
		    //document.getElementById('plataCalc3').value = "";
        }
        else
        {
    	    document.getElementById('amountCalc3').value = "" + M;
		    //document.getElementById('plataCalc3').value = "" + Plata;
        }
  	}
}

function power1(x,y)
{ total=1;
  for (j=0; j<y; j++)
   { total*=x; }
  return total; //result of x raised to y power
}

function compute_amountCalc8(form)
{
var vidDepozit=document.getElementById('ValutaCalc8').value;
var nacinIsplata=document.getElementById('nacinIsplataCalc8').value;
var iznos=document.getElementById('iznosCalc8').value;
var suma=0;
var osnova=Math.round(iznos*100)/100;
	
var kam1=kamata28;//var kam1=7.4;	
var kam2=kamata29;//var kam2=4.3;

if (iznos=='') 
{
	//alert(warningMsg3);
	document.getElementById('errordivCalc8').innerHTML = warningMsg3;
	document.getElementById('iznosCalc8').focus();
	return false;
}
else
{
    document.getElementById('errordivCalc8').innerHTML = "&nbsp;";
}
	
if (vidDepozit==1)
	{	
	if (iznos<30000) 
		{
		//alert(warningMsg4);
		document.getElementById('errordivCalc8').innerHTML = warningMsg4;
		form1.iznosCalc8.focus();
		return false;
		}
	else
	{
	    document.getElementById('errordivCalc8').innerHTML = "&nbsp;";
	}	
	kam1=kamata28;
	
	}
else
	{
	if (iznos<500) 
		{
		//alert(warningMsg5);
		//document.getElementById('errordivCalc8').style.display = "block";
		document.getElementById('errordivCalc8').innerHTML = warningMsg5;
		document.getElementById('iznosCalc8').focus();
		return false;
		}
	else
	{
	    document.getElementById('errordivCalc8').innerHTML = "&nbsp;";
	}	
	kam1=kamata29;
	}

if (nacinIsplata==1) 
		{
			for(i = 1; i < 13; i++) 
			{
			if(i!=12)
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*30/360);						
				//osnova+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*30/360);			
				suma+=osnova*kam1/100*30/360
				osnova=osnova+(osnova*kam1/100*30/360)
				}
			else
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*2*30/360);						
				//osnova+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*2*30/360);			
				suma+=osnova*kam1/100*2*30/360
				osnova=osnova+(osnova*kam1/100*30/360)
				}
			
			}
		}
	else
		{
			for(i = 1; i < 13; i++) 
			{
				if(i!=12)
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*30/360);			
				suma+=osnova*kam1/100*30/360					
				}
			else
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*2*30/360);										
				suma+=osnova*2*kam1/100*30/360										
				}			
			}
		}
	
    if(isNaN(suma))
    {
    	document.getElementById('kamataCalc8').value = "";
    }
    else
    {
	    document.getElementById('kamataCalc8').value=Math.round(suma*100)/100;
    }
	
}

function allowGrejsCalc4()
{
    var curr=document.getElementById('currencyCalc4').value
}

function compute_amountCalc4()
{
 	var P, R, N, K, M, T, Plt, Plata, Interest;
 
		P = eval(document.getElementById('KSCalc4').value);	

	if (document.getElementById('currencyCalc4').value=="1")
		Plt=kamata7;//Plt=0.35;
	if (document.getElementById('currencyCalc4').value=="2")
		Plt=kamata8;//Plt=0.28;
	if(document.getElementById('loanCalc4').value.length == 0)  
		document.getElementById('amountCalc4').value = "0.00";
	else {
    	Y = eval(document.getElementById('yearsCalc4').value);
    	K = eval(document.getElementById('loanCalc4').value);
	
	if(((K<3300) || (K>300000)))
	{
		//alert(warningMsg1); 
		//document.getElementById('errordivCalc4').style.display = "block";
		document.getElementById('errordivCalc4').innerHTML = warningMsg1;
		return false;
	}
	else
	{
	    document.getElementById('errordivCalc4').innerHTML = "&nbsp;";
	}

		T = Y*12;

		
			
			M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K			
			Plata = Math.round(M/Plt*100) / 100
			
				
      	M = Math.round(M*100) / 100;

        if(isNaN(M) && isNaN(Plata))
        {
    	    document.getElementById('amountCalc4').value = "";
		    //document.getElementById('plataCalc4').value = "";
        }
        else
        {
    	    document.getElementById('amountCalc4').value = "" + M;
		    //document.getElementById('plataCalc4').value = "" + Plata;
        }
  	}
}



function povikaj()
{
document.form1.action='Kalkulatori1.asp?kalk=4'
document.form1.submit();
}

function smeniGodini()
{
	document.auto.submit();
}

function promeniLoanCalc5()
{
document.getElementById('loanCalc5').value=document.getElementById('CarAmountCalc5').value-document.getElementById('UcestvoCalc5').value

}

function compute_amountCalc5()
{
 	var P, R, N, K, M, T, Plt, Plata, UcProcent,adminTrosoci,tip;
	UcProcent=document.getElementById('UcestvoCalc5').value*100/document.getElementById('CarAmountCalc5').value;
	adminTrosoci=document.getElementById('loanCalc5').value/100;

    if(document.getElementById('loanCalc5').value.length == 0)
		document.getElementById('amountCalc5').value = "0.00";
	else {
    	Y = eval(document.getElementById('yearsCalc5').value);
    	K = eval(document.getElementById('loanCalc5').value);  <!--+adminTrosoci-->
		P = eval(document.getElementById('KSCalc5').value);
	
    if((K<3000) || (K>30000) || (Y>7))
	{
		//alert(warningMsg1);
		//document.getElementById('errordiv1Calc5').style.display = "block";
		document.getElementById('errordiv1Calc5').innerHTML = warningMsg1;
        return false;
    }
    else
    {
        document.getElementById('errordiv1Calc5').innerHTML = "&nbsp;";
    }
	
	if(UcProcent<20) 
	{
		//alert(warningMsg6);
		//document.getElementById('errordivCalc5').style.display = "block";
        	document.getElementById('errordivCalc5').innerHTML = warningMsg6;
		return false;
	}
	else
	{
	    document.getElementById('errordivCalc5').innerHTML = "&nbsp;";
	    if (document.getElementById('CarAmountCalc5').value>15999  && UcProcent<25)
		{
		
			document.getElementById('errordivCalc5').innerHTML = warningMsg11;
			return false;
			
		}
		else
		{
		document.getElementById('errordivCalc5').innerHTML = "&nbsp;";	
		}
	}
	
	
	


	if (document.getElementById('currencyCalc5').value=="1")
		{
			Plt=kamata9;//Plt=0.35; 	
		}

	if(document.getElementById('currencyCalc5').value=="2")
		{
			
				if(UcProcent<30)
					{				
						Plt=kamata10;//Plt=0.35; 	
					}
				else
					{				
						Plt=kamata11;//Plt=0.45; 
					}		 
		}

	
		T = Y*12;
		M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K
		//M = K*P/12/100
		Plata = Math.round(M/Plt*100) / 100		
      	M = Math.round(M*100) / 100;

        if(isNaN(M) && isNaN(Plata))
        {
    	    document.getElementById('amountCalc5').value = "";
		    //document.getElementById('plataCalc5').value = "";
        }
        else
        {
    	    document.getElementById('amountCalc5').value = "" + M;
		    //document.getElementById('plataCalc5').value = "" + Plata;
        }
  	}
}
function promeniMaxiRata()
{
document.getElementById('maxiCalc10').value=maksiRata*document.getElementById('CarAmountCalc10').value;
}
function promeniLoanCalc10()
{
document.getElementById('loanCalc10').value=document.getElementById('CarAmountCalc10').value-document.getElementById('UcestvoCalc10').value
promeniMaxiRata();
}

function compute_amountCalc10()
{
 	var P, R, N, K, M, T, Plt, Plata, UcProcent,adminTrosoci,tip,MaxiRata,K1,poslednaRata;
    MaxiRata=maksiRata*document.getElementById('CarAmountCalc10').value;
	UcProcent=document.getElementById('UcestvoCalc10').value*100/document.getElementById('CarAmountCalc10').value;
	adminTrosoci=document.getElementById('loanCalc10').value/100;
    
    if(document.getElementById('loanCalc10').value.length == 0)
		document.getElementById('amountCalc10').value = "0.00";
	else {
    	Y = eval(document.getElementById('yearsCalc10').value);
        K1 = eval(document.getElementById('loanCalc10').value);
    	K = eval(document.getElementById('loanCalc10').value-MaxiRata);  //<!--+adminTrosoci-->
		P = eval(document.getElementById('KSCalc10').value);
	
    if(  (K1<3000) || (K1 > 30000) || (Y>6) || (Y<5) )
	{
		//alert(warningMsg1);
		//document.getElementById('errordiv1Calc5').style.display = "block";
		document.getElementById('errordiv1Calc10').innerHTML = warningMsg1;
        return false;
    }
    else
    {
        document.getElementById('errordiv1Calc10').innerHTML = "&nbsp;";
    }
	
	if(UcProcent<20) 
	{
		//alert(warningMsg6);
		//document.getElementById('errordivCalc5').style.display = "block";
        document.getElementById('errordivCalc10').innerHTML = warningMsg12;
		return false;
	}
	else
	{
	    document.getElementById('errordivCalc10').innerHTML = "&nbsp;";
	}
	
	
	


//	if (document.getElementById('currencyCalc10').value=="1")
//		{
//			Plt=kamata9;//Plt=0.35; 	
//		}

//	if(document.getElementById('currencyCalc10').value=="2")
//		{
//			
				if(UcProcent<30)
					{				
						Plt=kamata33;//Plt=0.35; 	
					}
				else
					{				
						Plt=kamata34;//Plt=0.45; 
					}		 
	//	}

	
		T = Y*12;
		M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K
		//M = K*P/12/100
		//Plata = Math.round(M/Plt*100) / 100
		
        Plata = (M+MaxiRata/T)/Plt;	
        Plata =  Math.round(Plata*100) / 100;		
		
      	M = Math.round(M*100) / 100;

        poslednaRata=MaxiRata+M;
        if(isNaN(M) && isNaN(Plata))
        {
    	    document.getElementById('amountCalc10').value = "";
            document.getElementById('lastAmountCalc10').value = "";
		    document.getElementById('plataCalc10').value = "";
            document.getElementById('lastAmountCalc10').value = "";
            
        }
        else
        {
    	    document.getElementById('amountCalc10').value = "" + M;
            document.getElementById('maxiCalc10').value = "" + MaxiRata;
		    document.getElementById('plataCalc10').value = "" + Plata;

            document.getElementById('lastAmountCalc10').value = "" + poslednaRata;
        }
  	}
}



function compute_amountCalc6()
{
    var period=document.getElementById('periodCalc6').value;
    var iznos=document.getElementById('iznosCalc6').value;
    if(iznos=='') 
    {
        //alert(warningMsg3);
        //document.getElementById('errordivCalc6').style.display = "block";
        document.getElementById('errordivCalc6').innerHTML = warningMsg3;
        document.getElementById('iznosCalc6').focus();
        return false;
    }
    else
    {
        document.getElementById('errordivCalc6').innerHTML = "&nbsp;";
    }

    if(iznos<35000) 
	{
		//alert(warningMsg7);
		//document.getElementById('errordivCalc6').style.display = "block";
		document.getElementById('errordivCalc6').innerHTML = warningMsg7;
		document.getElementById('iznosCalc6').focus();
		return false;
	}
	else
	{
	    document.getElementById('errordivCalc6').innerHTML = "&nbsp;";
	}
	kam1=kamata12;
	suma1=iznos*kam1*30/365/100	
	console.log("kam1:" + kam1);
	console.log("suma1:" + suma1);
	kam2=kamata13;
	suma2=iznos*kam2*30/365/100
	console.log("kam2:" + kam2);
	console.log("suma2:" + suma2);
	kam3=kamata14;
	suma3=iznos*kam3*30/365/100
	console.log("kam3:" + kam3);
	console.log("suma3:" + suma3);
	kam4=kamata15;
	suma4=iznos*kam4*30/365/100
	suma4=iznos*kam4*30/365/100
	console.log("kam4:" + kam4);
	console.log("suma4:" + suma4);
	//document.depoziti.kamata.value=Math.round(suma4*100)/100;
	//document.depoziti.kamata.value=Math.round((suma1+suma2+suma3+suma4)*100)/100
	document.getElementById('kamataCalc6').value = Math.round(suma1 + suma2 + suma3 + suma4);
}


function compute_amountCalc21()
{
    var period=document.getElementById('periodCalc21').value;
    var iznos=document.getElementById('iznosCalc21').value;
    if(iznos=='') 
    {
        //alert(warningMsg3);
        //document.getElementById('errordivCalc6').style.display = "block";
        document.getElementById('errordivCalc21').innerHTML = warningMsg3;
        document.getElementById('iznosCalc21').focus();
        return false;
    }
    else
    {
        document.getElementById('errordivCalc21').innerHTML = "&nbsp;";
    }

    var kamata;
    var nacinIsplata=document.getElementById('nacinIsplataCalc21').value;
    if(iznos<1000) 
    {
        //alert(warningMsg8);
        //document.getElementById('errordivCalc6').style.display = "block";
        document.getElementById('errordivCalc21').innerHTML = warningMsg8;
        document.getElementById('iznosCalc21').focus();
        return false;
    }
    else
    {
        document.getElementById('errordivCalc21').innerHTML = "&nbsp;";
    }
    var kam1=kamata16;
    var kam2=kamata17;
    var kam3=kamata18;
    console.log("Dekurzivno 3m");
    console.log("kam1:" + kam1);
	console.log("kam2:" + kam2);
	console.log("kam3:" + kam3);

    var kam4=kamata19;
    var kam5=kamata20;
    var kam6=kamata21;
    console.log("Dekurzivno 6m");
    console.log("kam4:" + kam4);
	console.log("kam5:" + kam5);
	console.log("kam6:" + kam6);

    var kam7=kamata22;
    var kam8=kamata23;
    var kam9=kamata24;
    console.log("Anticipativno 3m");
    console.log("kam7:" + kam7);
	console.log("kam8:" + kam8);
	console.log("kam9:" + kam9);

    var kam10=kamata25;
    var kam11=kamata26;
    var kam12=kamata27;
    console.log("Anticipativno 6m");
    console.log("kam10:" + kam10);
	console.log("kam11:" + kam11);
	console.log("kam12:" + kam12);
    


    if(iznos<=3000 && nacinIsplata==1 && period==3)
    {		
        kamata=iznos*kam1*30/360/100*3;	
    }

    if(iznos>3000 && iznos<=10000 && nacinIsplata==1 && period==3)
    {		
        kamata=(3000*kam1*30/360/100+(iznos-3000)*kam2*30/360/100)*3;
    }		
    if(iznos>10000 && nacinIsplata==1 && period==3)
    {		
        kamata=(3000*kam1*30/360/100+7000*kam2*30/360/100+(iznos-10000)*kam3*30/360/100)*3;
    }
        
    if(iznos<=3000 && nacinIsplata==1 && period==6)
    {		
        kamata=iznos*kam4*30/360/100*6;
    }
        
    if(iznos>3000 && iznos<=10000 && nacinIsplata==1 && period==6)
    {		
    kamata=(3000*kam4*30/360/100+(iznos-3000)*kam5*30/360/100)*6;		
    }
        
    if(iznos>10000 && nacinIsplata==1 && period==6)
    {		
        kamata=(3000*kam4*30/360/100+7000*kam5*30/360/100+(iznos-10000)*kam6*30/360/100)*6;
    }
        
    if(iznos<=3000 && nacinIsplata==2 && period==3)
    {		
        kamata=iznos*kam7*30/360/100*3;
    }
        
    if(iznos>3000 && iznos<=10000 && nacinIsplata==2 && period==3)
    {		
        kamata=(3000*kam7*30/360/100+(iznos-3000)*kam8*30/360/100)*3;
    }
        
    if(iznos>10000 && nacinIsplata==2 && period==3)
    {		
        kamata=(3000*kam7*30/360/100+7000*kam8*30/360/100+(iznos-10000)*kam9*30/360/100)*3		
    }
        
    if(iznos<=3000 && nacinIsplata==2 && period==6)
    {		
        kamata=iznos*kam10*30/360/100*6;
    }
        
    if(iznos>3000 && iznos<=10000 && nacinIsplata==2 && period==6)
    {		
        kamata=(3000*kam10*30/360/100+(iznos-3000)*kam11*30/360/100)*6;		
    }
        
    if(iznos>10000 && nacinIsplata==2 && period==6)
    {		
        kamata=(3000*kam10*30/360/100+7000*kam11*30/360/100+(iznos-10000)*kam12*30/360/100)*6;		
    }	
        
    if(isNaN(kamata))
    {
        document.getElementById('kamataCalc21').value = "";
    }
    else
    {		
        document.getElementById('kamataCalc21').value=Math.round(kamata*100)/100;
    }
}

function binomial(a, n)
{
  var i;
  var sum, pow, term, cof;
  if(n < 0)
    return 1.0 / binomial(a, -n);
  sum = 1.0;
  pow = n;
  term = 1;
  cof = 1.0
  for(i = 1; i < 10; i++) {
    cof = cof * pow / i;
    pow = pow - 1.0;
    term = term * a;
    sum = sum + cof * term;
  }
  return sum;
}

function error(P, I, N, Y, M)
{
  var value;
  value = P - M * (1.0 - binomial(I / Y, -N)) / (I / Y);
  return value;
}

/* Compute power */
function power(x,y)
{ total=1;
  for (j=0; j<y; j++)
   { total*=x; }
  return total; //result of x raised to y power
}

/* Compute Interest rate */
function compute_amountCalc7()
{
 	var P, R, N, K, M, T, Plt, Plata;

	if(document.getElementById('loanCalc7').value.length == 0)
		document.getElementById('amountCalc7').value = "0.00";
	else {
    	Y = eval(document.getElementById('monthsCalc7').value);
    	K = eval(document.getElementById('loanCalc7').value);
		P = eval(document.getElementById('KSCalc7').value);

	if((K<0) || (K>15000))
	{
		//alert(warningMsg1);
		//document.getElementById('errordivCalc7').style.display = "block";
		document.getElementById('errordivCalc7').innerHTML = warningMsg1;
        return false;
	}
	else
	{
	    document.getElementById('errordivCalc7').innerHTML = "&nbsp;";
	}
		
		T = Y;
		M = Math.pow((P/12/100+1),T)*P/12/100/(Math.pow((P/12/100+1),T)-1)*K
      	
		M = Math.round(M*100) / 100;
		
		if(isNaN(M))
        {
    	    document.getElementById('amountCalc7').value = "";
        }
        else
        {
    	    document.getElementById('amountCalc7').value = "" + M;	
        }	
		
  	}
}

function updateMonthsCalc7() 
{
	document.getElementById('monthsCalc7').options.length = 0
	if (document.getElementById('loanCalc7').value <= 5000) {
		for (i = 0; i < 24; i++)
		{ document.getElementById('monthsCalc7').options[i] = new Option(i + 1, i + 1) }

	}
	else {
		for (i = 0; i < 36; i++)
		{ document.getElementById('monthsCalc7').options[i] = new Option(i + 1, i + 1) }
	}
}

function power1(x,y)
{ total=1;
  for (j=0; j<y; j++)
   { total*=x; }
  return total; //result of x raised to y power
}

function compute_amountCalc9()
{
var vidDepozit=document.getElementById('ValutaCalc9').value;
var nacinIsplata=document.getElementById('nacinIsplataCalc9').value;
var iznos=document.getElementById('iznosCalc9').value;
var suma=0;
var osnova=Math.round(iznos*100)/100;
var kam1=kamata30;//var kam1=7.9;	
var kam2=kamata31;//var kam2=4.4;
if (iznos=='') 
{
	//alert(warningMsg3);
	//document.getElementById('errordivCalc9').style.display = "block";
	document.getElementById('errordivCalc9').innerHTML = warningMsg3;
	document.getElementById('iznosCalc9').focus();
	return false;
}
else
{
    document.getElementById('errordivCalc9').innerHTML = "&nbsp;";
}
if (vidDepozit==1)
	{	
	if (iznos<12000) 
	{
		//alert(warningMsg9);
		//document.getElementById('errordivCalc9').style.display = "block";
		document.getElementById('errordivCalc9').innerHTML = warningMsg9;
		document.getElementById('iznosCalc9').focus();
		return false;
	}
	else
	{
	    document.getElementById('errordivCalc1').innerHTML = "&nbsp;";
	}	
	kam1=kamata30;
	
	}
else
	{
	if (iznos<200) 
	{
		//alert(warningMsg10);
		//document.getElementById('errordivCalc9').style.display = "block";
		document.getElementById('errordivCalc9').innerHTML = warningMsg10;
		document.getElementById('iznosCalc9').focus();
		return false;
	}
	else
	{
	    document.getElementById('errordivCalc9').innerHTML = "&nbsp;";
	}	
	kam1=kamata31;
	}

if (nacinIsplata==1) 
		{
			for(i = 1; i < 19; i++) 
			{
			if(i!=18)
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*30/360);						
				//osnova+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*30/360);			
				suma+=osnova*kam1/100*30/360
				osnova=osnova+(osnova*kam1/100*30/360)
				}
			else
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*2*30/360);						
				//osnova+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*2*30/360);			
				suma+=osnova*kam1/100*3*30/360
				osnova=osnova+(osnova*kam1/100*30/360)
				}
			
			}
		}
	else
		{
			for(i = 1; i < 19; i++) 
			{
				if(i!=18)
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*30/360);			
				suma+=osnova*kam1/100*30/360					
				}
			else
				{
				//suma+=osnova*(12*(Math.pow((1+kam1/100),1/12)-1)*2*30/360);										
				suma+=osnova*3*kam1/100*30/360										
				}			
			}
		}
	
        if(isNaN(suma))
        {
    	    document.getElementById('kamataCalc9').value = "";
        }
        else
        {
	        document.getElementById('kamataCalc9').value=Math.round(suma*100)/100;
        }
	
}

function validate()
        {
            var loanCalc20 = parseFloat(document.getElementById("loanCalc20").value);
            var currencyCalc20 = document.getElementById("currencyCalc20").value;

            if(currencyCalc20 == "1")
            {
                if (!loanCalc20 || loanCalc20 < 300000 )
                {
                    document.getElementById("errordiv1calc20").innerHTML = "Минималниот износ за денарскиот депозит е 300.000 денари!"; 
                    return false;

                }
            }else if(currencyCalc20 == "2")
            {
                if (!loanCalc20 || loanCalc20 < 5000 )
                {
                    document.getElementById("errordiv1calc20").innerHTML = "Минималниот износ за девизниот депозит е 5000 евра!";
                    return false;
                }
            }
			document.getElementById("errordiv1calc20").innerHTML = "";	
            return true; 
        }
		
        function calculateDays(dateSubstract)
        {
			const date = new Date();
			var year = date.getFullYear() + dateSubstract;
			var month = date.getMonth() + 1;
			var day = date.getDate();
			const d = new Date(year+"-"+month+"-"+day);
		
			let difference = d.getTime() - date.getTime();
			let TotalDays = Math.ceil(difference / (1000 * 3600 * 24));
			console.log(TotalDays);
			return TotalDays;
        }
		
		function compute()
		{
            if(!validate()){
                return;
            }
			var nominalnaKamStap;
            var bonusKamStap;

            var isplatenaKamata;
            var isplatenaKamataFinal;

            var isplatenaBonusKamata;
            var isplatenaBonusKamataFinal;
            
            var dateSubstract;
            
            var yearsCalc20 = document.getElementById("yearsCalc20").value;
            var loanCalc20 = parseFloat(document.getElementById("loanCalc20").value);
            var currencyCalc20 = document.getElementById("currencyCalc20").value;

			dateSubstract =  parseInt(yearsCalc20);
			console.log(dateSubstract);
            
			if(currencyCalc20 == "1")
            {   
                if(yearsCalc20 == "1")
                {
                    nominalnaKamStap = 0.025;
                    bonusKamStap = 0;
                }else if (yearsCalc20 == "2")
                {
                    nominalnaKamStap = 0.031;
                    bonusKamStap = 0;
                }else if (yearsCalc20 == "3")
                {
                    nominalnaKamStap = 0.034;
                    bonusKamStap = 0;
                }
            }
            else if (currencyCalc20 == "2")
            {
                if(yearsCalc20 == "1")
                {
                    nominalnaKamStap = 0.015;
                    bonusKamStap = 0;
                }else if (yearsCalc20 == "2")
                {
                    nominalnaKamStap = 0.021;
                    bonusKamStap = 0;
                }else if (yearsCalc20 == "3")
                {
                    nominalnaKamStap = 0.024;
                    bonusKamStap = 0;
                }
            }
            var vkupno = 0;
			var totalDays = calculateDays(dateSubstract)/365;
			isplatenaKamata = loanCalc20 * nominalnaKamStap * totalDays;
            isplatenaKamataFinal = parseFloat(isplatenaKamata);
			isplatenaBonusKamata = loanCalc20 * bonusKamStap * totalDays;
            isplatenaBonusKamataFinal = parseFloat(isplatenaBonusKamata);
			vkupno = (isplatenaKamataFinal + isplatenaBonusKamataFinal);
			
            if(currencyCalc20 == "1")
            {   
                vkupno = vkupno.toFixed(0);

            }
			else 
            {
                vkupno = vkupno.toFixed(2);
            }

            document.getElementById("amountCalc20").value = vkupno;
			
		}
		
		function currency_changeCalc20()
		{

            document.getElementById('errordivcalc20').innerHTML = "&nbsp;";
            document.getElementById('errordiv1calc20').innerHTML = "&nbsp;";
            
            reset_inputText();
             if(document.getElementById('currencyCalc20').value=="1")
            {  
            document.getElementById('spanDenari20').innerHTML = denari;
            document.getElementById('spanDenari22').innerHTML = denari;
           // document.getElementById('spanDenari3').innerHTML = denari;
           // document.getElementById('pZabeleskaCalc1').style= "visibility:visible;";
           }
           else if(document.getElementById('currencyCalc20').value=="2")
           {
            document.getElementById('spanDenari20').innerHTML = "&#8364;";
            document.getElementById('spanDenari22').innerHTML = "&#8364;";
            //document.getElementById('spanDenari3').innerHTML = "&#8364;";
            //document.getElementById('pZabeleskaCalc1').style= "visibility:hidden;";
           }
        }