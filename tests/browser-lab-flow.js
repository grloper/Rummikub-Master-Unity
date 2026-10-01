async (page) => {
  const errors=[]; page.on('pageerror',e=>errors.push(e.message));
  await page.reload();
  await page.getByRole('button',{name:'Place orange 9',exact:true}).click();
  await page.getByRole('button',{name:'Check move',exact:true}).click();
  if(!await page.getByRole('button',{name:'Commit move'}).isDisabled()) throw Error('Short set enabled commit');
  await page.getByRole('button',{name:'Place orange 10',exact:true}).click();
  await page.getByRole('button',{name:'Place joker',exact:true}).click();
  await page.getByRole('button',{name:'Check move',exact:true}).click();
  if(await page.locator('#points').textContent()!=='30') throw Error('Wrong meld points');
  await page.getByRole('button',{name:'Commit move',exact:true}).click();
  if(await page.locator('#phase').textContent()!=='Committed') throw Error('Not committed');
  await page.getByRole('button',{name:'Place blue 7',exact:true}).click();
  if(!await page.getByRole('button',{name:'Commit move'}).isDisabled()) throw Error('Mixed set enabled commit');
  await page.getByRole('button',{name:'Undo draft',exact:true}).click();
  if(await page.locator('#remaining').textContent()!=='1') throw Error('Undo did not preserve committed tiles');
  await page.setViewportSize({width:1440,height:1050});
  await page.screenshot({path:'docs/images/structure-lab-desktop.png',fullPage:true,animations:'disabled'});
  const widths=[];
  for(const width of [320,390,768,1440]) {await page.setViewportSize({width,height:900}); widths.push(await page.evaluate(()=>({width:innerWidth,scroll:document.documentElement.scrollWidth})));}
  if(widths.some(x=>x.scroll>x.width)) throw Error('Page horizontal overflow '+JSON.stringify(widths));
  await page.setViewportSize({width:390,height:844});
  await page.screenshot({path:'docs/images/structure-lab-mobile.png',fullPage:true,animations:'disabled'});
  await page.getByRole('button',{name:'Reset fixture',exact:true}).click();
  await page.getByRole('button',{name:'Place orange 9',exact:true}).focus();
  await page.keyboard.press('Enter');
  if(await page.locator('#remaining').textContent()!=='3') throw Error('Keyboard activation failed');
  return {workflow:'short rejection, valid30 commit, mixed rejection, undo preserving committed state, reset, keyboard Enter',widths,errors};
}
