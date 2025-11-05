// Test script for TV navigation
// Run this in the browser console to test navigation

console.log('=== TV Navigation Test ===');

// Check if modules are loaded
console.log('1. Checking module availability...');
console.log('- debugNavigation available:', typeof window.debugNavigation !== 'undefined');
console.log('- enableTVNavigation available:', typeof window.enableTVNavigation !== 'undefined');
console.log('- checkTVNavigation available:', typeof window.checkTVNavigation !== 'undefined');

// Check current state
console.log('\n2. Current state...');
if (typeof window.checkTVNavigation !== 'undefined') {
  window.checkTVNavigation();
}

// Check body classes
console.log('- Body classes:', Array.from(document.body.classList));

// Check menu items
const menuItems = document.querySelectorAll('.menu-item');
console.log('- Menu items found:', menuItems.length);
menuItems.forEach((item, index) => {
  console.log(`  ${index}: ${item.textContent} - classes: ${Array.from(item.classList)}`);
});

// Try to enable navigation
console.log('\n3. Attempting to enable navigation...');
if (typeof window.debugNavigation !== 'undefined') {
  window.debugNavigation.enableKeyboard();
  console.log('- Keyboard navigation enabled via debug method');
}

if (typeof window.enableTVNavigation !== 'undefined') {
  window.enableTVNavigation();
  console.log('- TV navigation enabled via debug method');
}

// Force add classes manually
document.body.classList.add('tv-mode');
document.body.classList.add('keyboard-active');
console.log('- Manually added tv-mode and keyboard-active classes');

// Try to focus first menu item
if (menuItems.length > 0) {
  menuItems[0].classList.add('focused');
  menuItems[0].classList.add('active');
  console.log('- Manually added focused and active classes to first menu item');
}

console.log('\n4. Final state check...');
console.log('- Body classes:', Array.from(document.body.classList));
console.log('- First menu item classes:', menuItems[0] ? Array.from(menuItems[0].classList) : 'No menu items');

console.log('\n=== Test Complete ===');
console.log('Try pressing arrow keys now. If navigation works, you should see focus moving.');
console.log('If not, check the console for errors and verify the CSS is loaded.');