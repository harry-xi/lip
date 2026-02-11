import { defineConfig } from 'vitepress'

export default defineConfig({
  title: "lip",
  description: "A general package installer",
  themeConfig: {
    nav: [
      { text: 'Getting Started', link: '/getting-started' },
      { text: 'Commands', link: '/commands/install' },
      { text: 'Reference', link: '/reference/package-manifest' }
    ],

    sidebar: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' }
        ]
      },
      {
        text: 'Commands',
        items: [
          { text: 'lip init', link: '/commands/init' },
          { text: 'lip install', link: '/commands/install' },
          { text: 'lip uninstall', link: '/commands/uninstall' },
          { text: 'lip update', link: '/commands/update' },
          { text: 'lip list', link: '/commands/list' },
          { text: 'lip view', link: '/commands/view' },
          { text: 'lip version', link: '/commands/version' },
          { text: 'lip migrate', link: '/commands/migrate' },
          { text: 'lip cache clean', link: '/commands/cache-clean' },
          { text: 'lip config', link: '/commands/config' }
        ]
      },
      {
        text: 'Reference',
        items: [
          { text: 'Package Manifest', link: '/reference/package-manifest' },
          { text: 'Configuration', link: '/reference/configuration' }
        ]
      }
    ],

    socialLinks: [
      { icon: 'github', link: 'https://github.com/futrime/lip' }
    ]
  }
})
